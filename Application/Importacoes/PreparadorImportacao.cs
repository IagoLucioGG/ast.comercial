using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Importacoes;

public sealed record ErroLinha(int Linha, string Campo, string Mensagem, string? Valor);

public static class DuplicidadeModo
{
    public const string Permitir = "Permitir";
    public const string Ignorar = "Ignorar";
    public const string Atualizar = "Atualizar";

    public static string? Normalizar(string? valor) => Normalizador.Chave(valor) switch
    {
        "" or "permitir" or "criar" or "nenhuma" => Permitir,
        "ignorar" => Ignorar,
        "atualizar" => Atualizar,
        _ => null
    };
}

public sealed record OpcoesImportacao(string Duplicidade, string? Chave)
{
    public bool VerificaDuplicidade => Duplicidade != DuplicidadeModo.Permitir && Chave is not null;
}

/// <summary>Valor já convertido de um campo personalizado, pronto para gravar em ValorCampo.</summary>
public sealed class ValorPersonalizado
{
    public required CampoImportavel Campo { get; init; }
    public string? Texto { get; set; }
    public string? TextoGrande { get; set; }
    public int? Inteiro { get; set; }
    public decimal? Decimal { get; set; }
    public DateTime? DataHora { get; set; }
    public bool? Booleano { get; set; }
    public long? OpcaoId { get; set; }
    public string? OpcaoNome { get; set; }
}

public sealed class LinhaPreparada
{
    public int Numero { get; init; }
    /// <summary>Propriedade da entidade → valor tipado. Só contém colunas mapeadas e preenchidas.</summary>
    public Dictionary<string, object> Props { get; } = [];
    /// <summary>Valores aplicados só ao criar (ex.: funil/etapa padrão do negócio).</summary>
    public Dictionary<string, object> PropsCriacao { get; } = [];
    public List<ValorPersonalizado> Personalizados { get; } = [];
    public List<ErroLinha> Erros { get; } = [];
    public string? ChaveDuplicidade { get; set; }
    public bool Valida => Erros.Count == 0;
}

public sealed class ResultadoPreparo
{
    public required string Entidade { get; init; }
    public required string[] Cabecalhos { get; init; }
    public required List<LinhaPreparada> Linhas { get; init; }
    public required List<CampoImportavel> Campos { get; init; }
    /// <summary>Chave de duplicidade (normalizada) → Id existente. Nulo quando não se verifica duplicidade.</summary>
    public Dictionary<string, long>? Indice { get; init; }
}

/// <summary>
/// Converte as linhas do CSV em dados tipados, resolvendo referências por nome e acumulando erros por linha.
/// Não grava nada. Usa o AppDbContext da requisição/escopo (filtro de tenant ativo).
/// </summary>
public sealed class PreparadorImportacao(AppDbContext db)
{
    private const int MaxValorErro = 120;

    // ---------------------------------------------------------------- campos disponíveis

    /// <summary>Campos nativos (lista branca) + campos personalizados da empresa de tipos suportados.</summary>
    public async Task<List<CampoImportavel>> CamposDisponiveisAsync(string entidade, CancellationToken ct)
    {
        var lista = CatalogoImportacao.CamposNativos(entidade).ToList();
        var alvo = CatalogoImportacao.EntidadeAlvoDe(entidade);
        var campos = await db.Campos.AsNoTracking()
            .Where(c => c.EntidadeAlvo == alvo && !c.Nativo && !c.SomenteLeitura && c.Formula == null)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Chave, c.Tipo, c.Obrigatorio })
            .ToListAsync(ct);
        foreach (var c in campos)
        {
            if (!CatalogoImportacao.TipoPersonalizadoSuportado(c.Tipo)) continue;
            lista.Add(new CampoImportavel(
                $"campo:{c.Id}", c.Nome, CatalogoImportacao.TipoDadoDe(c.Tipo), false, 0,
                null, "Campo personalizado.", true, c.Id, c.Chave, c.Tipo));
        }
        return lista;
    }

    /// <summary>Sugere campo para cada coluna pela similaridade do nome. Cada campo é usado no máximo uma vez.</summary>
    public static List<(int Coluna, string Campo, double Pontuacao)> SugerirMapeamento(
        string[] cabecalhos, IReadOnlyList<CampoImportavel> campos)
    {
        var candidatos = new List<(int Coluna, string Campo, double Pontos)>();
        for (var i = 0; i < cabecalhos.Length; i++)
        {
            var h = Normalizador.Chave(cabecalhos[i]);
            if (h.Length == 0) continue;
            foreach (var campo in campos)
            {
                var nomes = new List<string> { Normalizador.Chave(campo.Rotulo), Normalizador.Chave(campo.Chave) };
                if (campo.Sinonimos is not null) nomes.AddRange(campo.Sinonimos.Select(Normalizador.Chave));
                var melhor = 0.0;
                for (var k = 0; k < nomes.Count; k++)
                {
                    var n = nomes[k];
                    if (n.Length == 0) continue;
                    var p = h == n ? 1.0 : Normalizador.Similaridade(h, n);
                    // O rótulo e a chave valem mais que sinônimos (que podem ser genéricos).
                    if (k >= 2 && p < 1.0) p *= 0.95;
                    melhor = Math.Max(melhor, p);
                }
                if (melhor >= 0.72) candidatos.Add((i, campo.Chave, melhor));
            }
        }

        var usadosColuna = new HashSet<int>();
        var usadosCampo = new HashSet<string>();
        var resultado = new List<(int, string, double)>();
        foreach (var c in candidatos.OrderByDescending(c => c.Pontos).ThenBy(c => c.Coluna))
        {
            if (usadosColuna.Contains(c.Coluna) || usadosCampo.Contains(c.Campo)) continue;
            usadosColuna.Add(c.Coluna);
            usadosCampo.Add(c.Campo);
            resultado.Add((c.Coluna, c.Campo, Math.Round(c.Pontos, 2)));
        }
        return resultado.OrderBy(r => r.Item1).ToList();
    }

    // ---------------------------------------------------------------- índice de duplicidade

    public async Task<Dictionary<string, long>> CarregarIndiceAsync(string entidade, string chave, CancellationToken ct)
    {
        var indice = new Dictionary<string, long>();
        switch (entidade)
        {
            case CatalogoEntidades.Cliente:
                foreach (var c in await db.Clientes.AsNoTracking().OrderBy(c => c.Id)
                             .Select(c => new { c.Id, c.Nome, c.Email, c.Documento }).ToListAsync(ct))
                    Adicionar(indice, ChaveDe(entidade, chave, null, c.Nome, c.Email, c.Documento, null), c.Id);
                break;
            case CatalogoEntidades.PessoaContato:
                foreach (var c in await db.PessoasContato.AsNoTracking().OrderBy(c => c.Id)
                             .Select(c => new { c.Id, c.ClienteId, c.Nome, c.Email, c.Documento }).ToListAsync(ct))
                    Adicionar(indice, ChaveDe(entidade, chave, c.ClienteId, c.Nome, c.Email, c.Documento, null), c.Id);
                break;
            case CatalogoEntidades.Produto:
                foreach (var p in await db.Produtos.AsNoTracking().OrderBy(p => p.Id)
                             .Select(p => new { p.Id, p.Nome, p.Codigo }).ToListAsync(ct))
                    Adicionar(indice, ChaveDe(entidade, chave, null, p.Nome, null, null, p.Codigo), p.Id);
                break;
            case CatalogoEntidades.Negocio:
                foreach (var n in await db.Negocios.AsNoTracking().OrderBy(n => n.Id)
                             .Select(n => new { n.Id, n.ClienteId, n.Titulo }).ToListAsync(ct))
                    Adicionar(indice, ChaveDe(entidade, chave, n.ClienteId, n.Titulo, null, null, null), n.Id);
                break;
        }
        return indice;
    }

    private static void Adicionar(Dictionary<string, long> indice, string? chave, long id)
    {
        if (chave is not null) indice.TryAdd(chave, id);
    }

    /// <summary>Valor normalizado da chave de duplicidade (nulo quando o dado não existe).</summary>
    public static string? ChaveDe(string entidade, string chave, long? clienteId, string? nome, string? email, string? documento, string? codigo)
    {
        switch (chave)
        {
            case "email":
                return string.IsNullOrWhiteSpace(email) ? null : "e|" + email.Trim().ToLowerInvariant();
            case "documento":
                var d = Normalizador.SomenteDigitos(documento);
                return d.Length == 0 ? null : "d|" + d;
            case "codigo":
                return string.IsNullOrWhiteSpace(codigo) ? null : "c|" + codigo.Trim().ToLowerInvariant();
            default:
                var n = Normalizador.Texto(nome);
                if (n.Length == 0) return null;
                if (entidade is CatalogoEntidades.PessoaContato or CatalogoEntidades.Negocio)
                    return clienteId is null ? null : $"n|{clienteId}|{n}";
                return "n|" + n;
        }
    }

    // ---------------------------------------------------------------- preparo das linhas

    public async Task<ResultadoPreparo> PrepararAsync(
        string entidade, CsvLido csv, Dictionary<int, string> mapeamento, OpcoesImportacao opcoes, CancellationToken ct,
        int? limiteLinhas = null)
    {
        var campos = await CamposDisponiveisAsync(entidade, ct);
        ValidarMapeamento(entidade, csv, mapeamento, opcoes, campos);

        var porChave = campos.ToDictionary(c => c.Chave);
        var cabecalhos = csv.Registros[0];
        var ctx = await CarregarReferenciasAsync(entidade, mapeamento, porChave, ct);

        var linhas = new List<LinhaPreparada>();
        var quantidade = limiteLinhas is null ? csv.Registros.Count - 1 : Math.Min(csv.Registros.Count - 1, limiteLinhas.Value);
        for (var i = 1; i <= quantidade; i++)
        {
            var reg = csv.Registros[i];
            var linha = new LinhaPreparada { Numero = i + 1 };
            PrepararLinha(entidade, linha, reg, cabecalhos.Length, mapeamento, porChave, ctx);
            linhas.Add(linha);
        }

        Dictionary<string, long>? indice = null;
        if (opcoes.VerificaDuplicidade)
            indice = await CarregarIndiceAsync(entidade, opcoes.Chave!, ct);

        return new ResultadoPreparo
        {
            Entidade = entidade, Cabecalhos = cabecalhos, Linhas = linhas, Campos = campos, Indice = indice
        };
    }

    private static void ValidarMapeamento(
        string entidade, CsvLido csv, Dictionary<int, string> mapeamento, OpcoesImportacao opcoes, List<CampoImportavel> campos)
    {
        var porChave = campos.ToDictionary(c => c.Chave);
        var largura = csv.Registros[0].Length;
        var usados = new HashSet<string>();
        foreach (var (coluna, chave) in mapeamento)
        {
            if (coluna < 0 || coluna >= largura)
                throw new RegraNegocioException($"A coluna {coluna + 1} do mapeamento não existe no arquivo.");
            if (!porChave.TryGetValue(chave, out var campo))
                throw new RegraNegocioException($"O campo '{chave}' não pode ser importado para {CatalogoImportacao.RotuloEntidade(entidade)}.");
            if (!usados.Add(chave))
                throw new RegraNegocioException($"O campo '{campo.Rotulo}' foi mapeado para mais de uma coluna.");
        }

        var faltando = campos.Where(c => c.Obrigatorio && !usados.Contains(c.Chave)).Select(c => c.Rotulo).ToList();
        if (faltando.Count > 0)
            throw new RegraNegocioException("Mapeie uma coluna para os campos obrigatórios: " + string.Join(", ", faltando) + ".");

        if (opcoes.Chave is not null)
        {
            var campoChave = CatalogoImportacao.CampoDaChave(entidade, opcoes.Chave);
            if (!usados.Contains(campoChave))
                throw new RegraNegocioException(
                    $"Para tratar duplicidade por '{opcoes.Chave}', mapeie uma coluna para o campo correspondente.");
            if (entidade is CatalogoEntidades.PessoaContato or CatalogoEntidades.Negocio && opcoes.Chave == "nome"
                && !usados.Contains("Cliente"))
                throw new RegraNegocioException("Para tratar duplicidade por nome, mapeie também a coluna do cliente.");
        }
    }

    // ---------------------------------------------------------------- referências

    private sealed class Referencias
    {
        public Dictionary<string, List<long>> StatusCliente { get; } = [];
        public Dictionary<string, List<long>> Origens { get; } = [];
        public Dictionary<string, List<long>> Ramos { get; } = [];
        public Dictionary<string, List<long>> Portes { get; } = [];
        public Dictionary<string, List<long>> Clientes { get; } = [];
        public Dictionary<string, List<long>> Contatos { get; } = [];   // "{clienteId}|{nome}"
        public Dictionary<string, List<long>> Familias { get; } = [];
        public Dictionary<string, List<(long Id, long? FamiliaId)>> Grupos { get; } = [];
        public Dictionary<string, List<long>> Moedas { get; } = [];
        public Dictionary<string, List<long>> Usuarios { get; } = [];
        public List<FunilRef> Funis { get; } = [];
        public Dictionary<long, Dictionary<long, string>> OpcoesPorCampo { get; } = []; // campoId → (opcaoId → rótulo)
    }

    private sealed record EtapaRef(long Id, string Nome, int Ordem);
    private sealed record FunilRef(long Id, string Nome, int Ordem, List<EtapaRef> Etapas);

    private static void Add(Dictionary<string, List<long>> d, string? nome, long id)
    {
        var n = Normalizador.Texto(nome);
        if (n.Length == 0) return;
        if (!d.TryGetValue(n, out var l)) d[n] = l = [];
        l.Add(id);
    }

    private async Task<Referencias> CarregarReferenciasAsync(
        string entidade, Dictionary<int, string> mapeamento, Dictionary<string, CampoImportavel> porChave, CancellationToken ct)
    {
        var r = new Referencias();
        var mapeados = mapeamento.Values.ToHashSet();

        if (mapeados.Contains("StatusCliente"))
            foreach (var x in await db.StatusClientes.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.StatusCliente, x.Nome, x.Id);
        if (mapeados.Contains("Origem"))
            foreach (var x in await db.OrigensContato.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.Origens, x.Nome, x.Id);
        if (mapeados.Contains("RamoAtividade"))
            foreach (var x in await db.RamosAtividade.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.Ramos, x.Nome, x.Id);
        if (mapeados.Contains("PorteEmpresa"))
            foreach (var x in await db.PortesEmpresa.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.Portes, x.Nome, x.Id);

        if (mapeados.Contains("Cliente"))
            foreach (var x in await db.Clientes.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.Clientes, x.Nome, x.Id);
        if (mapeados.Contains("PessoaContato"))
            foreach (var x in await db.PessoasContato.AsNoTracking().Select(x => new { x.Id, x.ClienteId, x.Nome }).ToListAsync(ct))
                Add(r.Contatos, $"{x.ClienteId}|{Normalizador.Texto(x.Nome)}", x.Id);

        if (mapeados.Contains("FamiliaProduto") || mapeados.Contains("GrupoProduto"))
            foreach (var x in await db.FamiliasProduto.AsNoTracking().Select(x => new { x.Id, x.Nome }).ToListAsync(ct)) Add(r.Familias, x.Nome, x.Id);
        if (mapeados.Contains("GrupoProduto"))
            foreach (var x in await db.GruposProduto.AsNoTracking().Select(x => new { x.Id, x.Nome, x.FamiliaProdutoId }).ToListAsync(ct))
            {
                var n = Normalizador.Texto(x.Nome);
                if (n.Length == 0) continue;
                if (!r.Grupos.TryGetValue(n, out var l)) r.Grupos[n] = l = [];
                l.Add((x.Id, x.FamiliaProdutoId));
            }

        if (mapeados.Contains("Moeda"))
            // Catálogo global (EmpresaId = 0) + moedas da própria empresa, igual ao MoedaServico.
            foreach (var x in await db.Moedas.IgnoreQueryFilters().AsNoTracking()
                         .Where(m => m.Ativo && (m.EmpresaId == 0 || m.EmpresaId == db.EmpresaIdAtual))
                         .Select(x => new { x.Id, x.Codigo, x.Nome }).ToListAsync(ct))
            {
                Add(r.Moedas, x.Codigo, x.Id);
                if (!string.Equals(x.Nome, x.Codigo, StringComparison.OrdinalIgnoreCase)) Add(r.Moedas, x.Nome, x.Id);
            }

        if (mapeados.Contains("Responsavel"))
            foreach (var x in await db.Usuarios.AsNoTracking().Select(x => new { x.Id, x.Nome, x.Email }).ToListAsync(ct))
            {
                Add(r.Usuarios, x.Email, x.Id);
                Add(r.Usuarios, x.Nome, x.Id);
            }

        if (entidade == CatalogoEntidades.Negocio)
        {
            var funis = await db.Funis.AsNoTracking().Include(f => f.Etapas).OrderBy(f => f.Ordem).ThenBy(f => f.Id).ToListAsync(ct);
            foreach (var f in funis)
                r.Funis.Add(new FunilRef(f.Id, f.Nome, f.Ordem,
                    f.Etapas.Where(e => e.Ativo).OrderBy(e => e.Ordem).ThenBy(e => e.Id).Select(e => new EtapaRef(e.Id, e.Nome, e.Ordem)).ToList()));
        }

        // Opções de campos personalizados do tipo Lista
        var idsLista = mapeados.Where(k => k.StartsWith("campo:")).Select(k => porChave[k])
            .Where(c => c.TipoPersonalizado == TipoCampo.Lista && c.CampoId is not null).Select(c => c.CampoId!.Value).ToList();
        if (idsLista.Count > 0)
        {
            foreach (var o in await db.OpcoesCampo.AsNoTracking().Where(o => idsLista.Contains(o.CampoId)).ToListAsync(ct))
            {
                if (!r.OpcoesPorCampo.TryGetValue(o.CampoId, out var d)) r.OpcoesPorCampo[o.CampoId] = d = [];
                d[o.Id] = string.IsNullOrWhiteSpace(o.Rotulo) ? o.Valor : o.Rotulo;
            }
        }
        return r;
    }

    // ---------------------------------------------------------------- linha

    private static void PrepararLinha(
        string entidade, LinhaPreparada linha, string[] reg, int largura,
        Dictionary<int, string> mapeamento, Dictionary<string, CampoImportavel> porChave, Referencias refs)
    {
        if (reg.Length > largura && reg.Skip(largura).Any(c => !string.IsNullOrWhiteSpace(c)))
            linha.Erros.Add(new ErroLinha(linha.Numero, "(linha)",
                $"A linha tem {reg.Length} colunas, mas o cabeçalho tem {largura}. Verifique aspas e separadores.", null));

        // valores crus por campo
        var cru = new Dictionary<string, string>();
        foreach (var (coluna, chave) in mapeamento)
            cru[chave] = coluna < reg.Length ? (reg[coluna] ?? string.Empty).Trim() : string.Empty;

        void Erro(string campo, string mensagem, string? valor = null)
            => linha.Erros.Add(new ErroLinha(linha.Numero, campo, mensagem,
                valor is null ? null : (valor.Length > MaxValorErro ? valor[..MaxValorErro] + "…" : valor)));

        // 1) campos que não são referência (tipos simples)
        foreach (var (chave, valor) in cru)
        {
            var campo = porChave[chave];
            if (campo.Personalizado || campo.Tipo == TipoDadoImportacao.Referencia) continue;

            if (valor.Length == 0)
            {
                if (campo.Obrigatorio) Erro(campo.Rotulo, "Campo obrigatório não preenchido.");
                continue;
            }
            if (campo.Tamanho > 0 && valor.Length > campo.Tamanho)
            {
                Erro(campo.Rotulo, $"Texto maior que o limite de {campo.Tamanho} caracteres.", valor);
                continue;
            }

            switch (campo.Tipo)
            {
                case TipoDadoImportacao.Texto:
                case TipoDadoImportacao.TextoLongo:
                    linha.Props[chave] = valor; break;
                case TipoDadoImportacao.Email:
                    if (Conversores.Email(valor, out var em, out var e1)) linha.Props[chave] = em.ToLowerInvariant(); else Erro(campo.Rotulo, e1!, valor);
                    break;
                case TipoDadoImportacao.Telefone:
                    if (Conversores.Telefone(valor, out var te, out var e2)) linha.Props[chave] = te; else Erro(campo.Rotulo, e2!, valor);
                    break;
                case TipoDadoImportacao.Documento:
                    if (Conversores.Documento(valor, out var dc, out var e3)) linha.Props[chave] = dc; else Erro(campo.Rotulo, e3!, valor);
                    break;
                case TipoDadoImportacao.Cep:
                    if (Conversores.Cep(valor, out var cp, out var e4)) linha.Props[chave] = cp; else Erro(campo.Rotulo, e4!, valor);
                    break;
                case TipoDadoImportacao.Uf:
                    if (Conversores.Uf(valor, out var uf, out var e5)) linha.Props[chave] = uf; else Erro(campo.Rotulo, e5!, valor);
                    break;
                case TipoDadoImportacao.Booleano:
                    if (Conversores.Booleano(valor, out var bo, out var e6)) linha.Props[chave] = bo; else Erro(campo.Rotulo, e6!, valor);
                    break;
                case TipoDadoImportacao.Decimal:
                    if (Conversores.Decimal(valor, out var de, out var e7)) linha.Props[chave] = de; else Erro(campo.Rotulo, e7!, valor);
                    break;
                case TipoDadoImportacao.Data:
                    if (Conversores.Data(valor, out var dt, out var e8)) linha.Props[chave] = dt; else Erro(campo.Rotulo, e8!, valor);
                    break;
                case TipoDadoImportacao.Categoria:
                    var cat = Normalizador.Chave(valor) switch
                    {
                        "produto" or "produtos" => (CategoriaProduto?)CategoriaProduto.Produto,
                        "servico" or "servicos" => CategoriaProduto.Servico,
                        "assinatura" or "assinaturas" => CategoriaProduto.Assinatura,
                        _ => null
                    };
                    if (cat is null) Erro(campo.Rotulo, "Categoria inválida (use Produto, Serviço ou Assinatura).", valor);
                    else linha.Props[chave] = cat.Value;
                    break;
            }
        }

        // 2) referências por nome
        long? clienteId = null;
        foreach (var (chave, valor) in cru)
        {
            var campo = porChave[chave];
            if (campo.Personalizado || campo.Tipo != TipoDadoImportacao.Referencia) continue;
            if (valor.Length == 0)
            {
                if (campo.Obrigatorio) Erro(campo.Rotulo, "Campo obrigatório não preenchido.");
                continue;
            }

            switch (chave)
            {
                case "StatusCliente": ResolverSimples(refs.StatusCliente, valor, campo, "StatusClienteId"); break;
                case "Origem": ResolverSimples(refs.Origens, valor, campo, "OrigemId"); break;
                case "RamoAtividade": ResolverSimples(refs.Ramos, valor, campo, "RamoAtividadeId"); break;
                case "PorteEmpresa": ResolverSimples(refs.Portes, valor, campo, "PorteEmpresaId"); break;
                case "Moeda": ResolverSimples(refs.Moedas, valor, campo, "MoedaId", "Moeda"); break;
                case "Responsavel": ResolverSimples(refs.Usuarios, valor, campo, "ResponsavelId", "Usuário"); break;
                case "FamiliaProduto": ResolverSimples(refs.Familias, valor, campo, "FamiliaProdutoId", "Família"); break;
                case "Cliente":
                    clienteId = ResolverSimples(refs.Clientes, valor, campo, "ClienteId", "Cliente");
                    break;
            }

        }

        long? ResolverSimples(Dictionary<string, List<long>> dic, string valor, CampoImportavel campo, string prop, string? tipo = null)
        {
            var nome = tipo ?? campo.Rotulo;
            if (!dic.TryGetValue(Normalizador.Texto(valor), out var ids))
            {
                Erro(campo.Rotulo, $"{nome} '{valor}' não encontrado(a). Cadastre antes de importar.", valor);
                return null;
            }
            var distintos = ids.Distinct().ToList();
            if (distintos.Count > 1)
            {
                Erro(campo.Rotulo, $"Há {distintos.Count} registros chamados '{valor}'. Use um nome único.", valor);
                return null;
            }
            linha.Props[prop] = distintos[0];
            return distintos[0];
        }

        // Grupo de produto (depende da família já resolvida)
        if (cru.TryGetValue("GrupoProduto", out var grupoValor) && grupoValor.Length > 0)
        {
            var campo = porChave["GrupoProduto"];
            if (!refs.Grupos.TryGetValue(Normalizador.Texto(grupoValor), out var grupos))
                Erro(campo.Rotulo, $"Grupo '{grupoValor}' não encontrado. Cadastre antes de importar.", grupoValor);
            else
            {
                var familiaId = linha.Props.TryGetValue("FamiliaProdutoId", out var f) ? (long?)f : null;
                var candidatos = familiaId is null ? grupos : grupos.Where(g => g.FamiliaId == familiaId || g.FamiliaId is null).ToList();
                if (candidatos.Count == 0) Erro(campo.Rotulo, $"O grupo '{grupoValor}' não pertence à família informada.", grupoValor);
                else if (candidatos.Select(g => g.Id).Distinct().Count() > 1) Erro(campo.Rotulo, $"Há mais de um grupo chamado '{grupoValor}'; informe também a família.", grupoValor);
                else linha.Props["GrupoProdutoId"] = candidatos[0].Id;
            }
        }

        if (entidade == CatalogoEntidades.Negocio)
        {
            // Contato do negócio (entre os contatos do cliente)
            if (cru.TryGetValue("PessoaContato", out var contatoValor) && contatoValor.Length > 0 && clienteId is not null)
            {
                var campo = porChave["PessoaContato"];
                if (!refs.Contatos.TryGetValue($"{clienteId}|{Normalizador.Texto(contatoValor)}", out var ids))
                    Erro(campo.Rotulo, $"Contato '{contatoValor}' não encontrado entre os contatos do cliente.", contatoValor);
                else if (ids.Distinct().Count() > 1)
                    Erro(campo.Rotulo, $"Há mais de um contato chamado '{contatoValor}' nesse cliente.", contatoValor);
                else linha.Props["PessoaContatoId"] = ids[0];
            }

            ResolverFunilEEtapa(linha, cru, porChave, refs, Erro);
        }

        // 3) campos personalizados
        foreach (var (chave, valor) in cru)
        {
            var campo = porChave[chave];
            if (!campo.Personalizado || valor.Length == 0) continue;
            PrepararPersonalizado(campo, valor, linha, refs, Erro);
        }
    }

    private static void ResolverFunilEEtapa(
        LinhaPreparada linha, Dictionary<string, string> cru, Dictionary<string, CampoImportavel> porChave,
        Referencias refs, Action<string, string, string?> erro)
    {
        cru.TryGetValue("Funil", out var funilNome);
        cru.TryGetValue("Etapa", out var etapaNome);
        funilNome = funilNome?.Trim() ?? "";
        etapaNome = etapaNome?.Trim() ?? "";

        FunilRef? funil = null;
        if (funilNome.Length > 0)
        {
            var achados = refs.Funis.Where(f => Normalizador.Texto(f.Nome) == Normalizador.Texto(funilNome)).ToList();
            if (achados.Count == 0) erro(porChave["Funil"].Rotulo, $"Funil '{funilNome}' não encontrado.", funilNome);
            else if (achados.Count > 1) erro(porChave["Funil"].Rotulo, $"Há mais de um funil chamado '{funilNome}'.", funilNome);
            else funil = achados[0];
            if (funil is null) return;
        }

        EtapaRef? etapa = null;
        if (etapaNome.Length > 0)
        {
            var alvo = Normalizador.Texto(etapaNome);
            if (funil is not null)
            {
                var achadas = funil.Etapas.Where(e => Normalizador.Texto(e.Nome) == alvo).ToList();
                if (achadas.Count == 0) { erro(porChave["Etapa"].Rotulo, $"Etapa '{etapaNome}' não existe no funil '{funil.Nome}'.", etapaNome); return; }
                etapa = achadas[0];
            }
            else
            {
                var achadas = refs.Funis.SelectMany(f => f.Etapas.Where(e => Normalizador.Texto(e.Nome) == alvo).Select(e => (Funil: f, Etapa: e))).ToList();
                if (achadas.Count == 0) { erro(porChave["Etapa"].Rotulo, $"Etapa '{etapaNome}' não encontrada em nenhum funil.", etapaNome); return; }
                if (achadas.Count > 1) { erro(porChave["Etapa"].Rotulo, $"A etapa '{etapaNome}' existe em mais de um funil; informe o funil.", etapaNome); return; }
                funil = achadas[0].Funil;
                etapa = achadas[0].Etapa;
            }
            linha.Props["FunilId"] = funil.Id;
            linha.Props["EtapaId"] = etapa.Id;
            return;
        }

        if (funil is not null)
        {
            if (funil.Etapas.Count == 0) { erro(porChave["Funil"].Rotulo, $"O funil '{funil.Nome}' não tem etapas.", funilNome); return; }
            linha.Props["FunilId"] = funil.Id;
            linha.Props["EtapaId"] = funil.Etapas[0].Id;
            return;
        }

        // Nada informado: só vale ao criar (primeiro funil, primeira etapa); ao atualizar mantém o que está.
        var padrao = refs.Funis.FirstOrDefault(f => f.Etapas.Count > 0);
        if (padrao is null) { erro("Funil", "A empresa não tem funil com etapas para receber o negócio.", null); return; }
        linha.PropsCriacao["FunilId"] = padrao.Id;
        linha.PropsCriacao["EtapaId"] = padrao.Etapas[0].Id;
    }

    private static void PrepararPersonalizado(
        CampoImportavel campo, string valor, LinhaPreparada linha, Referencias refs, Action<string, string, string?> erro)
    {
        var item = new ValorPersonalizado { Campo = campo };
        var nome = campo.Rotulo;
        switch (campo.TipoPersonalizado)
        {
            case TipoCampo.Texto:
                if (valor.Length > 500) { erro(nome, "Texto maior que o limite de 500 caracteres.", valor); return; }
                item.Texto = valor; break;
            case TipoCampo.TextoLongo:
                item.TextoGrande = valor; break;
            case TipoCampo.Email:
                if (!Conversores.Email(valor, out var em, out var e1)) { erro(nome, e1!, valor); return; }
                item.Texto = em; break;
            case TipoCampo.Telefone:
                if (!Conversores.Telefone(valor, out var te, out var e2)) { erro(nome, e2!, valor); return; }
                item.Texto = te; break;
            case TipoCampo.Cpf:
            case TipoCampo.Cnpj:
                if (!Conversores.Documento(valor, out var dc, out var e3)) { erro(nome, e3!, valor); return; }
                item.Texto = dc; break;
            case TipoCampo.Cep:
                if (!Conversores.Cep(valor, out var cp, out var e4)) { erro(nome, e4!, valor); return; }
                item.Texto = cp; break;
            case TipoCampo.Numero:
                if (!Conversores.Inteiro(valor, out var n, out var e5)) { erro(nome, e5!, valor); return; }
                item.Inteiro = n; break;
            case TipoCampo.Decimal:
            case TipoCampo.Moeda:
            case TipoCampo.Percentagem:
                if (!Conversores.Decimal(valor, out var d, out var e6, permitirNegativo: true, casas: 4)) { erro(nome, e6!, valor); return; }
                item.Decimal = d; break;
            case TipoCampo.Data:
            case TipoCampo.DataHora:
                if (!Conversores.Data(valor, out var dt, out var e7)) { erro(nome, e7!, valor); return; }
                item.DataHora = dt; break;
            case TipoCampo.Booleano:
                if (!Conversores.Booleano(valor, out var b, out var e8)) { erro(nome, e8!, valor); return; }
                item.Booleano = b; break;
            case TipoCampo.Lista:
                if (campo.CampoId is null || !refs.OpcoesPorCampo.TryGetValue(campo.CampoId.Value, out var opcoes))
                { erro(nome, "O campo de lista não tem opções cadastradas.", valor); return; }
                var alvo = Normalizador.Texto(valor);
                var achada = opcoes.Where(o => Normalizador.Texto(o.Value) == alvo).Select(o => (long?)o.Key).FirstOrDefault();
                if (achada is null) { erro(nome, $"A opção '{valor}' não existe nesta lista.", valor); return; }
                item.OpcaoId = achada;
                item.OpcaoNome = opcoes[achada.Value];
                break;
            default:
                erro(nome, "Tipo de campo não suportado na importação.", valor); return;
        }
        linha.Personalizados.Add(item);
    }

    /// <summary>Define ChaveDuplicidade de cada linha válida conforme a chave escolhida.</summary>
    public static void CalcularChaves(string entidade, string chave, IEnumerable<LinhaPreparada> linhas)
    {
        foreach (var l in linhas)
        {
            if (!l.Valida) continue;
            string? Texto(string p) => l.Props.TryGetValue(p, out var v) ? v as string : null;
            long? clienteId = l.Props.TryGetValue("ClienteId", out var c) ? (long?)c : null;
            var nome = entidade == CatalogoEntidades.Negocio ? Texto("Titulo") : Texto("Nome");
            l.ChaveDuplicidade = ChaveDe(entidade, chave, clienteId, nome, Texto("Email"), Texto("Documento"), Texto("Codigo"));
        }
    }
}

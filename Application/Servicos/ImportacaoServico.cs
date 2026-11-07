using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AST.Comercial.Application.Importacoes;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ImportacaoServico(AppDbContext db, ExecutorImportacao executor) : IImportacaoServico
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan TempoMaximoProcessando = TimeSpan.FromHours(1);

    private PreparadorImportacao Preparador => new(db);

    private static string EntidadeOuErro(string? entidade)
        => CatalogoImportacao.ResolverEntidade(entidade)
           ?? throw new RegraNegocioException("Entidade inválida. Use Cliente, PessoaContato, Produto ou Negocio.");

    // ------------------------------------------------------------------ passo 1: analisar

    public async Task<AnaliseImportacaoDto> AnalisarAsync(string entidade, string nomeArquivo, byte[] bytes, CancellationToken ct = default)
    {
        entidade = EntidadeOuErro(entidade);
        var csv = CsvLeitor.Ler(bytes);
        var campos = await Preparador.CamposDisponiveisAsync(entidade, ct);

        var largura = csv.Registros[0].Length;
        var cabecalhos = csv.Registros[0]
            .Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Coluna {i + 1}" : h.Trim()).ToArray();
        var amostra = csv.Registros.Skip(1).Take(10)
            .Select(r => Enumerable.Range(0, largura).Select(i => i < r.Length ? r[i].Trim() : string.Empty).ToArray())
            .ToArray();

        var sugestao = PreparadorImportacao.SugerirMapeamento(cabecalhos, campos)
            .Select(s => new SugestaoMapeamentoDto(s.Coluna, cabecalhos[s.Coluna], s.Campo, s.Pontuacao)).ToList();

        return new AnaliseImportacaoDto(
            entidade, CatalogoImportacao.RotuloEntidade(entidade), NomeSeguro(nomeArquivo), csv.Codificacao,
            csv.Separador == '\t' ? "tab" : csv.Separador.ToString(),
            csv.Registros.Count - 1, cabecalhos, amostra,
            campos.Select(c => new CampoImportavelDto(c.Chave, c.Rotulo, c.TipoNome, c.Obrigatorio, c.Personalizado, c.Tamanho, c.Dica)).ToList(),
            sugestao,
            CatalogoImportacao.ChavesDuplicidade(entidade).Select(k => new ChaveDuplicidadeDto(k, RotuloChave(k))).ToList());
    }

    private static string RotuloChave(string chave) => chave switch
    {
        "email" => "E-mail",
        "documento" => "Documento (CPF/CNPJ)",
        "codigo" => "Código",
        _ => "Nome"
    };

    // ------------------------------------------------------------------ passo 2: validar

    public async Task<ValidacaoImportacaoDto> ValidarAsync(
        string entidade, byte[] bytes, string? mapeamentoJson, string? duplicidade, string? chave, CancellationToken ct = default)
    {
        entidade = EntidadeOuErro(entidade);
        var csv = CsvLeitor.Ler(bytes);
        var mapeamento = LerMapeamento(mapeamentoJson);
        var opcoes = LerOpcoes(entidade, duplicidade, chave, permitirSemModo: true);

        var preparo = await Preparador.PrepararAsync(entidade, csv, mapeamento, opcoes, ct);
        if (opcoes.Chave is not null) PreparadorImportacao.CalcularChaves(entidade, opcoes.Chave, preparo.Linhas);

        var erros = preparo.Linhas.Where(l => !l.Valida).SelectMany(l => l.Erros).ToList();
        var validas = preparo.Linhas.Count(l => l.Valida);

        var duplicadas = 0;
        if (opcoes.Chave is not null && preparo.Indice is not null)
        {
            var vistas = new HashSet<string>();
            foreach (var l in preparo.Linhas.Where(l => l.Valida && l.ChaveDuplicidade is not null))
                if (preparo.Indice.ContainsKey(l.ChaveDuplicidade!) | !vistas.Add(l.ChaveDuplicidade!)) duplicadas++;
        }

        const int limite = 1000;
        return new ValidacaoImportacaoDto(
            preparo.Linhas.Count, validas, preparo.Linhas.Count - validas, duplicadas, erros.Count, erros.Count > limite,
            erros.Take(limite).ToList());
    }

    // ------------------------------------------------------------------ passo 3: executar

    public async Task<ImportacaoDto> ExecutarAsync(
        string entidade, string nomeArquivo, byte[] bytes, string? mapeamentoJson, string? duplicidade, string? chave,
        CancellationToken ct = default)
    {
        entidade = EntidadeOuErro(entidade);
        var csv = CsvLeitor.Ler(bytes);
        var mapeamento = LerMapeamento(mapeamentoJson);
        var opcoes = LerOpcoes(entidade, duplicidade, chave, permitirSemModo: false);

        var preparo = await Preparador.PrepararAsync(entidade, csv, mapeamento, opcoes, ct);
        if (opcoes.Chave is not null) PreparadorImportacao.CalcularChaves(entidade, opcoes.Chave, preparo.Linhas);

        var invalidas = preparo.Linhas.Where(l => !l.Valida).ToList();
        var mapaBase = new JsonObject
        {
            ["mapeamento"] = new JsonObject(mapeamento.OrderBy(m => m.Key)
                .Select(m => KeyValuePair.Create(m.Key.ToString(), (JsonNode?)JsonValue.Create(m.Value)))),
            ["opcoes"] = new JsonObject
            {
                ["duplicidade"] = opcoes.Duplicidade,
                ["chave"] = opcoes.Chave
            },
            ["arquivo"] = new JsonObject
            {
                ["codificacao"] = csv.Codificacao,
                ["separador"] = csv.Separador == '\t' ? "tab" : csv.Separador.ToString()
            }
        };

        var importacao = new Importacao
        {
            EmpresaId = db.EmpresaIdAtual,
            Entidade = entidade,
            NomeArquivo = NomeSeguro(nomeArquivo),
            Status = StatusImportacao.Processando,
            TotalLinhas = preparo.Linhas.Count,
            Sucesso = 0,
            Falhas = invalidas.Count,
            MapeamentoJson = mapaBase.ToJsonString(),
            ErrosJson = ExecutorImportacao.SerializarErros(invalidas.SelectMany(l => l.Erros)),
            UsuarioId = db.UsuarioIdAtual
        };
        db.Importacoes.Add(importacao);
        await db.SaveChangesAsync(ct);

        if (preparo.Linhas.All(l => !l.Valida))
        {
            // Nada a gravar: encerra já, sem segundo plano.
            importacao.Status = StatusImportacao.Falhou;
            importacao.ConcluidaEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        else
        {
            var ctx = new ContextoExecucao(
                db.EmpresaIdAtual, db.UsuarioIdAtual, db.UsuarioNomeAtual ?? db.UsuarioAtual, db.PerfilAtual,
                importacao.Id, entidade, opcoes, mapaBase);
            executor.Iniciar(ctx, preparo, preparo.Linhas.Count);
        }

        return await ObterAsync(importacao.Id, ct) ?? throw new KeyNotFoundException();
    }

    // ------------------------------------------------------------------ histórico

    public async Task<(List<ImportacaoDto> Itens, int Total)> ListarAsync(int pagina, int tamanho, CancellationToken ct = default)
    {
        await EncerrarTravadasAsync(ct);
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);

        var total = await db.Importacoes.AsNoTracking().CountAsync(ct);
        var itens = await db.Importacoes.AsNoTracking()
            .OrderByDescending(i => i.CriadoEm).ThenByDescending(i => i.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(i => new { Imp = i, Usuario = i.Usuario != null ? i.Usuario.Nome : null })
            .ToListAsync(ct);
        return (itens.Select(x => Mapear(x.Imp, x.Usuario)).ToList(), total);
    }

    public async Task<ImportacaoDto?> ObterAsync(long id, CancellationToken ct = default)
    {
        var x = await db.Importacoes.AsNoTracking().Where(i => i.Id == id)
            .Select(i => new { Imp = i, Usuario = i.Usuario != null ? i.Usuario.Nome : null })
            .FirstOrDefaultAsync(ct);
        return x is null ? null : Mapear(x.Imp, x.Usuario);
    }

    /// <summary>Uma importação "Processando" há muito tempo perdeu o processo (reinício do servidor): marca como falha.</summary>
    private async Task EncerrarTravadasAsync(CancellationToken ct)
    {
        var limite = DateTime.UtcNow - TempoMaximoProcessando;
        var travadas = await db.Importacoes
            .Where(i => i.Status == StatusImportacao.Processando && (i.AtualizadoEm ?? i.CriadoEm) < limite)
            .ToListAsync(ct);
        if (travadas.Count == 0) return;
        foreach (var i in travadas)
        {
            i.Status = StatusImportacao.Falhou;
            i.ConcluidaEm = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static ImportacaoDto Mapear(Importacao i, string? usuarioNome)
    {
        int criadas = 0, atualizadas = 0, ignoradas = 0, processadas = 0;
        var duplicidade = DuplicidadeModo.Permitir;
        string? chave = null;
        try
        {
            if (JsonNode.Parse(i.MapeamentoJson) is JsonObject raiz)
            {
                if (raiz["resumo"] is JsonObject r)
                {
                    criadas = (int?)r["criadas"] ?? 0;
                    atualizadas = (int?)r["atualizadas"] ?? 0;
                    ignoradas = (int?)r["ignoradas"] ?? 0;
                    processadas = (int?)r["processadas"] ?? 0;
                }
                if (raiz["opcoes"] is JsonObject o)
                {
                    duplicidade = (string?)o["duplicidade"] ?? DuplicidadeModo.Permitir;
                    chave = (string?)o["chave"];
                }
            }
        }
        catch (JsonException) { /* registros antigos/sem JSON válido */ }

        if (i.Status is StatusImportacao.Concluida or StatusImportacao.ConcluidaComErros or StatusImportacao.Falhou && processadas == 0)
            processadas = i.TotalLinhas;

        return new ImportacaoDto(
            i.Id, i.Entidade, CatalogoImportacao.RotuloEntidade(i.Entidade), i.NomeArquivo, i.Status.ToString(),
            i.TotalLinhas, i.Sucesso, i.Falhas, criadas, atualizadas, ignoradas, processadas, duplicidade, chave,
            i.Falhas > 0 || (i.ErrosJson.Length > 2), usuarioNome, i.CriadoEm, i.ConcluidaEm);
    }

    // ------------------------------------------------------------------ relatório de erros e modelo

    public async Task<string?> GerarRelatorioErrosAsync(long id, CancellationToken ct = default)
    {
        var imp = await db.Importacoes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (imp is null) return null;

        List<ErroLinha> erros;
        try { erros = JsonSerializer.Deserialize<List<ErroLinha>>(imp.ErrosJson, Json) ?? []; }
        catch (JsonException) { erros = []; }

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.Append(CsvLeitor.Linha(["Linha", "Campo", "Erro", "Valor informado"])).Append("\r\n");
        foreach (var e in erros.OrderBy(e => e.Linha))
            sb.Append(CsvLeitor.Linha([e.Linha.ToString(), e.Campo, e.Mensagem, e.Valor])).Append("\r\n");
        return sb.ToString();
    }

    public async Task<string> GerarModeloAsync(string entidade, CancellationToken ct = default)
    {
        entidade = EntidadeOuErro(entidade);
        var campos = await Preparador.CamposDisponiveisAsync(entidade, ct);
        // Cabeçalhos fixos gerados pelo sistema; ainda assim passam pela proteção de fórmula (nome de campo personalizado é livre).
        return "\uFEFF" + CsvLeitor.Linha(campos.Select(c => c.Rotulo)) + "\r\n";
    }

    // ------------------------------------------------------------------ entradas

    private static string NomeSeguro(string? nome)
    {
        // Só o nome exibido no histórico: sem diretórios, sem caracteres de controle.
        var n = Path.GetFileName((nome ?? "arquivo.csv").Replace('\\', '/'));
        n = new string(n.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (n.Length == 0) n = "arquivo.csv";
        return n.Length > 200 ? n[..200] : n;
    }

    private static Dictionary<int, string> LerMapeamento(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new RegraNegocioException("Informe o mapeamento de colunas.");
        if (json.Length > 100_000) throw new RegraNegocioException("Mapeamento grande demais.");

        var resultado = new Dictionary<int, string>();
        try
        {
            if (JsonNode.Parse(json) is not JsonObject obj)
                throw new RegraNegocioException("Mapeamento inválido: esperado um objeto {\"coluna\": \"campo\"}.");
            foreach (var (chave, valor) in obj)
            {
                var campo = valor?.GetValueKind() == JsonValueKind.String ? valor.GetValue<string>().Trim() : null;
                if (string.IsNullOrEmpty(campo)) continue; // coluna ignorada
                if (!int.TryParse(chave, out var coluna))
                    throw new RegraNegocioException($"Coluna '{chave}' inválida no mapeamento (use o número da coluna, começando em 0).");
                resultado[coluna] = campo;
            }
        }
        catch (JsonException)
        {
            throw new RegraNegocioException("Mapeamento inválido (JSON malformado).");
        }
        if (resultado.Count == 0) throw new RegraNegocioException("Mapeie pelo menos uma coluna.");
        return resultado;
    }

    private static OpcoesImportacao LerOpcoes(string entidade, string? duplicidade, string? chave, bool permitirSemModo)
    {
        var modo = DuplicidadeModo.Normalizar(duplicidade)
                   ?? throw new RegraNegocioException("Opção de duplicidade inválida. Use Ignorar, Atualizar ou Permitir.");
        string? chaveNorm = null;
        if (!string.IsNullOrWhiteSpace(chave))
        {
            chaveNorm = Normalizador.Chave(chave) switch { "codigo" => "codigo", "documento" => "documento", "email" => "email", "nome" or "titulo" => "nome", _ => null };
            if (chaveNorm is null || !CatalogoImportacao.ChavesDuplicidade(entidade).Contains(chaveNorm))
                throw new RegraNegocioException(
                    $"Chave de duplicidade inválida para {CatalogoImportacao.RotuloEntidade(entidade)}. Opções: {string.Join(", ", CatalogoImportacao.ChavesDuplicidade(entidade))}.");
        }
        if (modo != DuplicidadeModo.Permitir && chaveNorm is null)
            throw new RegraNegocioException("Informe a chave de duplicidade (e-mail, documento, código ou nome).");
        // Chave sem modo: na validação serve só para contar duplicidades; ao executar, o mais seguro é não duplicar.
        if (modo == DuplicidadeModo.Permitir && chaveNorm is not null)
            modo = DuplicidadeModo.Ignorar;
        return new OpcoesImportacao(modo, chaveNorm);
    }
}

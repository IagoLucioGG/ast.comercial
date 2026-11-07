using System.Globalization;
using System.Text.Json;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

/// <summary>
/// Fluxos de aprovação (cadastro) e motor de aprovação de propostas.
///
/// Regras:
/// - Fluxos ativos da empresa são avaliados por Ordem (depois Id); o primeiro que se enquadra vale.
/// - Filtros do mesmo Grupo = E; grupos diferentes = OU; sem filtros = sempre se enquadra.
/// - Tipo Alerta nunca gera solicitação; Bloqueio e ControleAlcada geram, e precisam de ao menos 1 nível com aprovador.
/// - Solicitação em andamento = Status Pendente/AprovadoNivel e Ativo. Ativo=false numa solicitação em andamento = Cancelada.
/// - Cada decisão por nível é gravada em RegistrosAlteracao (Acao = "AprovacaoDecisao") porque a solicitação guarda só a última.
/// </summary>
public class FluxoAprovacaoServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IFluxoAprovacaoServico
{
    private const string AcaoDecisao = "AprovacaoDecisao";

    // =====================================================================
    // OData (compatível)
    // =====================================================================

    public IQueryable<FluxoAprovacao> ObterTodos() => db.FluxosAprovacao.AsNoTracking();

    public async Task<FluxoAprovacao?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.FluxosAprovacao.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<FluxoAprovacao> CriarAsync(Delta<FluxoAprovacao> delta, CancellationToken ct = default)
    {
        var entidade = new FluxoAprovacao { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        entidade.EmpresaId = db.EmpresaIdAtual;
        db.FluxosAprovacao.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<FluxoAprovacao?> AtualizarAsync(long id, Delta<FluxoAprovacao> delta, CancellationToken ct = default)
    {
        var entidade = await db.FluxosAprovacao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return null;
        var empresa = entidade.EmpresaId;
        delta.Patch(entidade);
        entidade.EmpresaId = empresa;
        if (!entidade.Ativo) await CancelarEmAndamentoDoFluxoAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.FluxosAprovacao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await CancelarEmAndamentoDoFluxoAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<NivelAprovacao> ObterNiveis() => db.NiveisAprovacao.AsNoTracking();

    public IQueryable<SolicitacaoAprovacao> ObterSolicitacoes() => db.SolicitacoesAprovacao.AsNoTracking();

    public async Task AprovarSolicitacaoAsync(long solicitacaoId, string? comentario, CancellationToken ct = default)
        => await DecidirSolicitacaoAsync(solicitacaoId, true, comentario, ct);

    public async Task RejeitarSolicitacaoAsync(long solicitacaoId, string? comentario, CancellationToken ct = default)
        => await DecidirSolicitacaoAsync(solicitacaoId, false, comentario, ct);

    // =====================================================================
    // CRUD completo (api/fluxos-aprovacao)
    // =====================================================================

    private static readonly string[] OpsNumericos = ["Igual", "Diferente", "Maior", "Menor", "MaiorOuIgual", "MenorOuIgual", "Vazio", "NaoVazio"];
    private static readonly string[] OpsId = ["Igual", "Diferente", "Vazio", "NaoVazio"];

    private static readonly CampoFiltroAprovacaoDto[] Campos =
    [
        new("DescontoPercentual", "Desconto total (%)", "numero", OpsNumericos,
            "Soma dos descontos dos itens e do desconto da proposta, em % do valor bruto dos itens."),
        new("DescontoMaximoItem", "Maior desconto de um item (%)", "numero", OpsNumericos, "Maior desconto % entre os itens da proposta."),
        new("Valor", "Valor total", "numero", OpsNumericos, "Valor da proposta (na moeda dela; combine com o filtro de moeda)."),
        new("CustoFrete", "Frete", "numero", OpsNumericos, "Custo de frete da proposta."),
        new("NumeroParcelas", "Número de parcelas", "numero", OpsNumericos, "Quantidade de parcelas."),
        new("ResponsavelId", "Responsável", "usuario", OpsId, "Id do usuário responsável; vários separados por vírgula."),
        new("ClienteId", "Cliente", "cliente", OpsId, "Id do cliente; vários separados por vírgula."),
        new("ProdutoId", "Produto (em qualquer item)", "produto", OpsId, "Id do produto; vários separados por vírgula. Igual = algum item é de um deles."),
        new("MoedaId", "Moeda", "moeda", OpsId, "Id da moeda; vários separados por vírgula."),
    ];

    public IReadOnlyList<CampoFiltroAprovacaoDto> CamposFiltro() => Campos;

    public async Task<List<FluxoAprovacaoDto>> ListarFluxosAsync(CancellationToken ct = default)
    {
        var t = db.EmpresaIdAtual;
        var ids = await db.FluxosAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.EmpresaId == t && f.EntidadeAlvo == EntidadeAlvo.Proposta)
            .OrderBy(f => f.Ordem).ThenBy(f => f.Id).Select(f => f.Id).ToListAsync(ct);
        var fluxos = await CarregarFluxosAsync(ids, ct);
        var nomes = await NomesUsuariosAsync(fluxos.Values.SelectMany(f => f.Niveis).SelectMany(n => n.Aprovadores)
            .Where(a => a.UsuarioId != null).Select(a => a.UsuarioId!.Value), ct);
        return ids.Where(fluxos.ContainsKey).Select(i => ParaDto(fluxos[i], nomes)).ToList();
    }

    public async Task<FluxoAprovacaoDto?> ObterFluxoAsync(long id, CancellationToken ct = default)
    {
        var fluxos = await CarregarFluxosAsync([id], ct);
        if (!fluxos.TryGetValue(id, out var f)) return null;
        var nomes = await NomesUsuariosAsync(f.Niveis.SelectMany(n => n.Aprovadores).Where(a => a.UsuarioId != null).Select(a => a.UsuarioId!.Value), ct);
        return ParaDto(f, nomes);
    }

    public async Task<FluxoAprovacaoDto> CriarFluxoAsync(FluxoAprovacaoDto entrada, CancellationToken ct = default)
    {
        var tipo = await ValidarFluxoAsync(entrada, ct);
        var fluxo = new FluxoAprovacao
        {
            EmpresaId = db.EmpresaIdAtual,
            EntidadeAlvo = EntidadeAlvo.Proposta,
            Ativo = entrada.Ativo ?? true
        };
        AplicarEntrada(fluxo, entrada, tipo);
        db.FluxosAprovacao.Add(fluxo);
        await db.SaveChangesAsync(ct);
        return (await ObterFluxoAsync(fluxo.Id, ct))!;
    }

    public async Task<FluxoAprovacaoDto?> AtualizarFluxoAsync(long id, FluxoAprovacaoDto entrada, CancellationToken ct = default)
    {
        var t = db.EmpresaIdAtual;
        var fluxo = await db.FluxosAprovacao.IgnoreQueryFilters()
            .Include(f => f.Filtros).Include(f => f.Niveis).ThenInclude(n => n.Aprovadores)
            .FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == t && f.EntidadeAlvo == EntidadeAlvo.Proposta, ct);
        if (fluxo is null) return null;

        var tipo = await ValidarFluxoAsync(entrada, ct);

        db.RemoveRange(fluxo.Niveis.SelectMany(n => n.Aprovadores));
        db.RemoveRange(fluxo.Niveis);
        db.RemoveRange(fluxo.Filtros);
        fluxo.Niveis = [];
        fluxo.Filtros = [];

        AplicarEntrada(fluxo, entrada, tipo);
        if (entrada.Ativo is { } ativo) fluxo.Ativo = ativo;
        if (!fluxo.Ativo) await CancelarEmAndamentoDoFluxoAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return await ObterFluxoAsync(id, ct);
    }

    public async Task<bool> ExcluirFluxoAsync(long id, CancellationToken ct = default)
    {
        var t = db.EmpresaIdAtual;
        var fluxo = await db.FluxosAprovacao.IgnoreQueryFilters()
            .Include(f => f.Filtros).Include(f => f.Niveis).ThenInclude(n => n.Aprovadores)
            .FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == t && f.EntidadeAlvo == EntidadeAlvo.Proposta, ct);
        if (fluxo is null) return false;

        var usado = await db.SolicitacoesAprovacao.IgnoreQueryFilters().AnyAsync(s => s.FluxoAprovacaoId == id, ct);
        if (usado)
            throw new RegraNegocioException("Este fluxo já tem solicitações no histórico e não pode ser excluído. Desative-o para que deixe de ser aplicado.");

        db.RemoveRange(fluxo.Niveis.SelectMany(n => n.Aprovadores));
        db.RemoveRange(fluxo.Niveis);
        db.RemoveRange(fluxo.Filtros);
        db.FluxosAprovacao.Remove(fluxo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void AplicarEntrada(FluxoAprovacao fluxo, FluxoAprovacaoDto e, TipoFluxoAprovacao tipo)
    {
        fluxo.Nome = e.Nome.Trim();
        fluxo.Tipo = tipo;
        fluxo.Mensagem = string.IsNullOrWhiteSpace(e.Mensagem) ? null : e.Mensagem.Trim();
        fluxo.Ordem = e.Ordem;

        var ordem = 0;
        foreach (var f in e.Filtros)
        {
            fluxo.Filtros.Add(new FiltroFluxoAprovacao
            {
                EmpresaId = fluxo.EmpresaId,
                Grupo = f.Grupo,
                Ordem = ordem++,
                CampoReferencia = Campos.First(c => c.Campo.Equals(f.CampoReferencia.Trim(), StringComparison.OrdinalIgnoreCase)).Campo,
                Operador = Enum.Parse<OperadorFiltro>(f.Operador.Trim(), true),
                Valor = string.IsNullOrWhiteSpace(f.Valor) ? null : f.Valor.Trim()
            });
        }

        var numero = 1;
        foreach (var n in e.Niveis)
        {
            var nivel = new NivelAprovacao
            {
                EmpresaId = fluxo.EmpresaId,
                Nivel = numero++,
                Descricao = string.IsNullOrWhiteSpace(n.Descricao) ? null : n.Descricao.Trim()
            };
            foreach (var a in n.Aprovadores)
                nivel.Aprovadores.Add(new AprovadorNivel
                {
                    EmpresaId = fluxo.EmpresaId,
                    UsuarioId = a.UsuarioId,
                    Perfil = a.UsuarioId is null && !string.IsNullOrWhiteSpace(a.Perfil) ? Enum.Parse<PerfilUsuario>(a.Perfil.Trim(), true) : null
                });
            fluxo.Niveis.Add(nivel);
        }
    }

    private async Task<TipoFluxoAprovacao> ValidarFluxoAsync(FluxoAprovacaoDto e, CancellationToken ct)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Nome)) erros.Add("Informe o nome do fluxo.");
        else if (e.Nome.Trim().Length > 300) erros.Add("O nome pode ter até 300 caracteres.");
        if (e.Mensagem is { Length: > 500 }) erros.Add("A mensagem pode ter até 500 caracteres.");
        if (e.Ordem is < 0 or > 100000) erros.Add("A ordem deve estar entre 0 e 100000.");

        if (!Enum.TryParse<TipoFluxoAprovacao>(e.Tipo?.Trim(), true, out var tipo) || !Enum.IsDefined(tipo))
        {
            erros.Add("Tipo inválido (use Alerta, Bloqueio ou ControleAlcada).");
            tipo = TipoFluxoAprovacao.ControleAlcada;
        }

        e.Filtros ??= [];
        e.Niveis ??= [];
        if (e.Filtros.Count > 30) erros.Add("Use no máximo 30 filtros.");
        for (var i = 0; i < Math.Min(e.Filtros.Count, 30); i++)
        {
            var f = e.Filtros[i];
            var p = $"Filtro {i + 1}";
            if (f.Grupo is < 0 or > 100) { erros.Add($"{p}: o grupo deve estar entre 0 e 100."); continue; }
            var campo = Campos.FirstOrDefault(c => c.Campo.Equals(f.CampoReferencia?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (campo is null) { erros.Add($"{p}: campo inválido."); continue; }
            if (!Enum.TryParse<OperadorFiltro>(f.Operador?.Trim(), true, out var op) || !campo.Operadores.Contains(op.ToString()))
            { erros.Add($"{p}: operador inválido para '{campo.Rotulo}'."); continue; }
            if (op is OperadorFiltro.Vazio or OperadorFiltro.NaoVazio) continue;
            if (string.IsNullOrWhiteSpace(f.Valor)) { erros.Add($"{p}: informe o valor."); continue; }
            if (f.Valor.Length > 1000) { erros.Add($"{p}: valor muito longo."); continue; }
            if (campo.Tipo == "numero" ? !TentarDecimal(f.Valor, out _) : ParseIds(f.Valor) is null)
                erros.Add($"{p}: valor inválido para '{campo.Rotulo}'.");
        }

        if (e.Niveis.Count > 10) erros.Add("Use no máximo 10 níveis.");
        var usuarioIds = new HashSet<long>();
        for (var i = 0; i < Math.Min(e.Niveis.Count, 10); i++)
        {
            var n = e.Niveis[i];
            n.Aprovadores ??= [];
            var p = $"Nível {i + 1}";
            if (n.Descricao is { Length: > 200 }) erros.Add($"{p}: a descrição pode ter até 200 caracteres.");
            if (n.Aprovadores.Count == 0) erros.Add($"{p}: informe ao menos um aprovador.");
            if (n.Aprovadores.Count > 30) erros.Add($"{p}: use no máximo 30 aprovadores.");
            foreach (var a in n.Aprovadores.Take(30))
            {
                if (a.UsuarioId is { } uid) usuarioIds.Add(uid);
                else if (!Enum.TryParse<PerfilUsuario>(a.Perfil?.Trim(), true, out var pf) || !Enum.IsDefined(pf))
                    erros.Add($"{p}: cada aprovador deve ser um usuário ou um perfil válido.");
            }
        }
        if (tipo != TipoFluxoAprovacao.Alerta && e.Niveis.Count == 0)
            erros.Add("Fluxos de bloqueio ou controle de alçada precisam de ao menos um nível de aprovação.");

        if (usuarioIds.Count > 0)
        {
            var validos = await db.Usuarios.Where(u => usuarioIds.Contains(u.Id) && u.Tipo == TipoUsuario.Normal)
                .Select(u => u.Id).ToListAsync(ct);
            if (validos.Count != usuarioIds.Count) erros.Add("Há aprovador que não é um usuário ativo da empresa.");
        }

        if (erros.Count > 0) throw new RegraNegocioException(string.Join(" ", erros.Take(5)));
        return tipo;
    }

    private static FluxoAprovacaoDto ParaDto(FluxoAprovacao f, Dictionary<long, string> nomes) => new()
    {
        Id = f.Id,
        Nome = f.Nome,
        Tipo = f.Tipo.ToString(),
        Mensagem = f.Mensagem,
        Ordem = f.Ordem,
        Ativo = f.Ativo,
        Filtros = f.Filtros.OrderBy(x => x.Grupo).ThenBy(x => x.Ordem).Select(x => new FiltroFluxoAprovacaoDto
        {
            Grupo = x.Grupo,
            CampoReferencia = x.CampoReferencia,
            Operador = x.Operador.ToString(),
            Valor = x.Valor
        }).ToList(),
        Niveis = f.Niveis.OrderBy(n => n.Nivel).Select(n => new NivelFluxoAprovacaoDto
        {
            Nivel = n.Nivel,
            Descricao = n.Descricao,
            Aprovadores = n.Aprovadores.OrderBy(a => a.Id).Select(a => new AprovadorNivelDto
            {
                UsuarioId = a.UsuarioId,
                UsuarioNome = a.UsuarioId is { } u && nomes.TryGetValue(u, out var nome) ? nome : null,
                Perfil = a.Perfil?.ToString()
            }).ToList()
        }).ToList()
    };

    /// <summary>Carrega fluxos com filtros, níveis e aprovadores ativos, mesmo que o fluxo esteja inativo (tenant conferido na mão).</summary>
    private async Task<Dictionary<long, FluxoAprovacao>> CarregarFluxosAsync(IEnumerable<long> ids, CancellationToken ct, bool somenteAtivos = false)
    {
        var t = db.EmpresaIdAtual;
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return [];

        var fluxos = await db.FluxosAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.EmpresaId == t && lista.Contains(f.Id) && (!somenteAtivos || f.Ativo)).ToListAsync(ct);
        var filtros = await db.FiltrosFluxoAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.EmpresaId == t && x.Ativo && lista.Contains(x.FluxoAprovacaoId)).ToListAsync(ct);
        var niveis = await db.NiveisAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.EmpresaId == t && n.Ativo && lista.Contains(n.FluxoAprovacaoId)).ToListAsync(ct);
        var nivelIds = niveis.Select(n => n.Id).ToList();
        var aprovadores = nivelIds.Count == 0 ? [] : await db.AprovadoresNivel.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.EmpresaId == t && a.Ativo && nivelIds.Contains(a.NivelAprovacaoId)).ToListAsync(ct);

        foreach (var n in niveis) n.Aprovadores = aprovadores.Where(a => a.NivelAprovacaoId == n.Id).ToList();
        foreach (var f in fluxos)
        {
            f.Filtros = filtros.Where(x => x.FluxoAprovacaoId == f.Id).ToList();
            f.Niveis = niveis.Where(n => n.FluxoAprovacaoId == f.Id).OrderBy(n => n.Nivel).ToList();
        }
        return fluxos.ToDictionary(f => f.Id);
    }

    private async Task<Dictionary<long, string>> NomesUsuariosAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var t = db.EmpresaIdAtual;
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return [];
        return await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.EmpresaId == t && lista.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
    }

    /// <summary>Cancela as solicitações em andamento do fluxo (desativado) e solta as propostas que aguardavam.</summary>
    private async Task CancelarEmAndamentoDoFluxoAsync(long fluxoId, CancellationToken ct)
    {
        var abertas = await db.SolicitacoesAprovacao
            .Where(s => s.FluxoAprovacaoId == fluxoId && (s.Status == StatusSolicitacaoAprovacao.Pendente || s.Status == StatusSolicitacaoAprovacao.AprovadoNivel))
            .ToListAsync(ct);
        if (abertas.Count == 0) return;
        var propostaIds = abertas.Select(s => s.RegistroId).ToList();
        var propostas = await db.Propostas.Where(p => propostaIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var s in abertas)
        {
            Cancelar(s, "Cancelada: o fluxo foi desativado.");
            var p = propostas.FirstOrDefault(x => x.Id == s.RegistroId);
            if (p is { StatusAprovacao: StatusAprovacaoProposta.Pendente }) p.StatusAprovacao = null;
        }
    }

    private static void Cancelar(SolicitacaoAprovacao s, string motivo)
    {
        s.Ativo = false;
        s.Observacao = Truncar(string.IsNullOrWhiteSpace(s.Observacao) ? motivo : $"{s.Observacao} | {motivo}", 1000);
    }

    // =====================================================================
    // Avaliação dos filtros
    // =====================================================================

    private sealed record DadosProposta(
        decimal? Valor, decimal DescontoPercentual, decimal DescontoMaximoItem, decimal? CustoFrete, int? NumeroParcelas,
        long? ResponsavelId, long? ClienteId, long? MoedaId, HashSet<long> Produtos);

    private static DadosProposta Metricas(Proposta p, IReadOnlyCollection<ItemProposta> itens)
    {
        decimal bruto = 0, liquidoItens = 0, maiorDesc = 0;
        foreach (var i in itens)
        {
            var b = (decimal)(i.Quantidade ?? 0) * (i.PrecoUnitario ?? 0m);
            var d = Math.Clamp((decimal)(i.Desconto ?? 0), 0m, 100m);
            bruto += b;
            liquidoItens += b * (1m - d / 100m);
            if (d > maiorDesc) maiorDesc = d;
        }
        var descProposta = p.Desconto ?? 0m;
        if (itens.Count == 0) bruto = Math.Max(0m, (p.Valor ?? 0m) - (p.CustoFrete ?? 0m) + descProposta);
        var descontoTotal = itens.Count == 0 ? descProposta : bruto - liquidoItens + descProposta;
        var pct = bruto > 0m ? Math.Round(descontoTotal / bruto * 100m, 2, MidpointRounding.AwayFromZero) : 0m;
        if (pct < 0m) pct = 0m;

        decimal? valor = p.Valor;
        if (valor is null && itens.Count > 0) valor = Math.Max(0m, liquidoItens - descProposta + (p.CustoFrete ?? 0m));
        return new DadosProposta(valor, pct, maiorDesc, p.CustoFrete, p.NumeroParcelas, p.ResponsavelId, p.ClienteId, p.MoedaId,
            itens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).ToHashSet());
    }

    private static bool TentarDecimal(string? s, out decimal valor)
        => decimal.TryParse(s?.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor);

    private static HashSet<long>? ParseIds(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var set = new HashSet<long>();
        foreach (var parte in s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(parte, NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
            set.Add(id);
        }
        return set.Count == 0 ? null : set;
    }

    private static bool AvaliarFiltro(FiltroFluxoAprovacao f, DadosProposta d)
    {
        var op = f.Operador;
        if (f.CampoReferencia.Equals("ProdutoId", StringComparison.OrdinalIgnoreCase))
        {
            if (op == OperadorFiltro.Vazio) return d.Produtos.Count == 0;
            if (op == OperadorFiltro.NaoVazio) return d.Produtos.Count > 0;
            var alvo = ParseIds(f.Valor);
            if (alvo is null) return false;
            var algum = d.Produtos.Overlaps(alvo);
            return op switch { OperadorFiltro.Igual => algum, OperadorFiltro.Diferente => !algum, _ => false };
        }

        if (f.CampoReferencia.Equals("ResponsavelId", StringComparison.OrdinalIgnoreCase)) return AvaliarId(op, d.ResponsavelId, f.Valor);
        if (f.CampoReferencia.Equals("ClienteId", StringComparison.OrdinalIgnoreCase)) return AvaliarId(op, d.ClienteId, f.Valor);
        if (f.CampoReferencia.Equals("MoedaId", StringComparison.OrdinalIgnoreCase)) return AvaliarId(op, d.MoedaId, f.Valor);

        decimal? atual = f.CampoReferencia.ToLowerInvariant() switch
        {
            "descontopercentual" => d.DescontoPercentual,
            "descontomaximoitem" => d.DescontoMaximoItem,
            "valor" => d.Valor,
            "custofrete" => d.CustoFrete,
            "numeroparcelas" => d.NumeroParcelas,
            _ => null
        };
        if (op == OperadorFiltro.Vazio) return atual is null;
        if (op == OperadorFiltro.NaoVazio) return atual is not null;
        if (!TentarDecimal(f.Valor, out var alvoNum)) return false;
        if (atual is null) return op == OperadorFiltro.Diferente;
        return op switch
        {
            OperadorFiltro.Igual => atual == alvoNum,
            OperadorFiltro.Diferente => atual != alvoNum,
            OperadorFiltro.Maior => atual > alvoNum,
            OperadorFiltro.Menor => atual < alvoNum,
            OperadorFiltro.MaiorOuIgual => atual >= alvoNum,
            OperadorFiltro.MenorOuIgual => atual <= alvoNum,
            _ => false
        };
    }

    private static bool AvaliarId(OperadorFiltro op, long? atual, string? valor)
    {
        if (op == OperadorFiltro.Vazio) return atual is null;
        if (op == OperadorFiltro.NaoVazio) return atual is not null;
        var alvo = ParseIds(valor);
        if (alvo is null) return false;
        var contem = atual is { } a && alvo.Contains(a);
        return op switch { OperadorFiltro.Igual => contem, OperadorFiltro.Diferente => !contem, _ => false };
    }

    private static bool Enquadra(FluxoAprovacao fluxo, DadosProposta d)
    {
        var filtros = fluxo.Filtros.OrderBy(f => f.Grupo).ThenBy(f => f.Ordem).ToList();
        if (filtros.Count == 0) return true;
        return filtros.GroupBy(f => f.Grupo).Any(g => g.All(f => AvaliarFiltro(f, d)));
    }

    private static bool FluxoUtil(FluxoAprovacao f)
        => f.Tipo != TipoFluxoAprovacao.Alerta && f.Niveis.Any(n => n.Aprovadores.Count > 0);

    private async Task<(FluxoAprovacao? Fluxo, DadosProposta Dados)> EncontrarFluxoAsync(Proposta p, CancellationToken ct)
    {
        var itens = await db.ItensProposta.AsNoTracking().Where(i => i.PropostaId == p.Id).ToListAsync(ct);
        var dados = Metricas(p, itens);

        var ids = await db.FluxosAprovacao.AsNoTracking()
            .Where(f => f.EntidadeAlvo == EntidadeAlvo.Proposta && f.Tipo != TipoFluxoAprovacao.Alerta)
            .OrderBy(f => f.Ordem).ThenBy(f => f.Id).Select(f => f.Id).ToListAsync(ct);
        var fluxos = await CarregarFluxosAsync(ids, ct, somenteAtivos: true);
        foreach (var id in ids)
            if (fluxos.TryGetValue(id, out var f) && FluxoUtil(f) && Enquadra(f, dados))
                return (f, dados);
        return (null, dados);
    }

    // =====================================================================
    // Motor
    // =====================================================================

    private Task<SolicitacaoAprovacao?> EmAndamentoAsync(long propostaId, CancellationToken ct)
        => db.SolicitacoesAprovacao
            .Where(s => s.EntidadeAlvo == EntidadeAlvo.Proposta && s.RegistroId == propostaId
                && (s.Status == StatusSolicitacaoAprovacao.Pendente || s.Status == StatusSolicitacaoAprovacao.AprovadoNivel))
            .OrderByDescending(s => s.Id).FirstOrDefaultAsync(ct);

    public async Task<bool> PropostaGovernadaPorFluxoAsync(long propostaId, CancellationToken ct = default)
    {
        if (await EmAndamentoAsync(propostaId, ct) is not null) return true;
        var p = await db.Propostas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == propostaId, ct);
        if (p is null || p.IsTemplate) return false;
        return (await EncontrarFluxoAsync(p, ct)).Fluxo is not null;
    }

    public async Task AvaliarPropostaAsync(long propostaId, CancellationToken ct = default)
    {
        var p = await db.Propostas.FirstOrDefaultAsync(x => x.Id == propostaId, ct);
        if (p is null) return;
        await AvaliarInternoAsync(p, explicito: false, null, ct);
    }

    public async Task<SolicitacaoAprovacaoDto> SolicitarAprovacaoPropostaAsync(long propostaId, string? observacao, CancellationToken ct = default)
    {
        var p = await db.Propostas.FirstOrDefaultAsync(x => x.Id == propostaId, ct)
            ?? throw new KeyNotFoundException($"Proposta {propostaId} não encontrada.");
        var sol = await AvaliarInternoAsync(p, explicito: true, observacao, ct)
            ?? throw new RegraNegocioException("Nenhum fluxo de aprovação ativo se aplica a esta proposta.");
        return (await MontarAsync([sol], ct))[0];
    }

    private async Task<SolicitacaoAprovacao?> AvaliarInternoAsync(Proposta p, bool explicito, string? observacao, CancellationToken ct)
    {
        if (p.IsTemplate)
        {
            if (explicito) throw new RegraNegocioException("Modelos de proposta não passam por aprovação.");
            return null;
        }
        if (observacao is { Length: > 1000 }) throw new RegraNegocioException("A observação pode ter até 1000 caracteres.");

        var (fluxo, _) = await EncontrarFluxoAsync(p, ct);
        var ativa = await EmAndamentoAsync(p.Id, ct);
        var ultima = await db.SolicitacoesAprovacao
            .Where(s => s.EntidadeAlvo == EntidadeAlvo.Proposta && s.RegistroId == p.Id)
            .OrderByDescending(s => s.Id).FirstOrDefaultAsync(ct);

        if (ativa is not null)
        {
            var semDecisao = ativa.Status == StatusSolicitacaoAprovacao.Pendente && ativa.NivelAtual == 1;
            if (fluxo is not null && fluxo.Id == ativa.FluxoAprovacaoId)
            {
                if (explicito)
                    throw new RegraNegocioException($"Esta proposta já aguarda aprovação (nível {ativa.NivelAtual} de {fluxo.Niveis.Count}).");
                if (semDecisao) return ativa; // nada decidido ainda: a mesma solicitação segue valendo
            }
            if (fluxo is null && explicito)
                throw new RegraNegocioException("Nenhum fluxo de aprovação ativo se aplica a esta proposta.");
            Cancelar(ativa, fluxo is null
                ? "Cancelada: a proposta deixou de se enquadrar em fluxo de aprovação."
                : "Reiniciada: a proposta foi alterada durante a aprovação.");
            if (p.StatusAprovacao == StatusAprovacaoProposta.Pendente) p.StatusAprovacao = null;
        }
        else if (explicito && fluxo is not null && p.StatusAprovacao == StatusAprovacaoProposta.Aprovada
                 && ultima is { Status: StatusSolicitacaoAprovacao.Aprovado })
            throw new RegraNegocioException("A proposta já está aprovada. Altere valores, descontos ou itens para reabrir a avaliação.");

        if (fluxo is null)
        {
            if (explicito) throw new RegraNegocioException("Nenhum fluxo de aprovação ativo se aplica a esta proposta.");
            // Rejeitada por fluxo e que não exige mais aprovação: libera (sem confundir com recusa do cliente).
            if (p.StatusAprovacao == StatusAprovacaoProposta.Rejeitada && p.StatusResposta != StatusRespostaProposta.Recusada
                && ultima is { Status: StatusSolicitacaoAprovacao.Rejeitado })
                p.StatusAprovacao = null;
            await db.SaveChangesAsync(ct);
            return null;
        }

        var nova = new SolicitacaoAprovacao
        {
            EmpresaId = p.EmpresaId,
            FluxoAprovacaoId = fluxo.Id,
            RegistroId = p.Id,
            EntidadeAlvo = EntidadeAlvo.Proposta,
            NivelAtual = 1,
            Status = StatusSolicitacaoAprovacao.Pendente,
            SolicitadoPorId = db.UsuarioIdAtual,
            Observacao = Truncar(string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(), 1000)
        };
        db.SolicitacoesAprovacao.Add(nova);
        p.StatusAprovacao = StatusAprovacaoProposta.Pendente;
        p.AprovadorId = null;
        await db.SaveChangesAsync(ct);
        return nova;
    }

    public async Task<bool> DecidirPropostaAsync(long propostaId, bool aprovar, string? comentario, CancellationToken ct = default)
    {
        var p = await db.Propostas.FirstOrDefaultAsync(x => x.Id == propostaId, ct)
            ?? throw new KeyNotFoundException($"Proposta {propostaId} não encontrada.");
        var ativa = await EmAndamentoAsync(propostaId, ct);
        if (ativa is null)
        {
            if (p.IsTemplate) return false;
            var (fluxo, _) = await EncontrarFluxoAsync(p, ct);
            if (fluxo is null) return false;

            var ultima = await db.SolicitacoesAprovacao
                .Where(s => s.EntidadeAlvo == EntidadeAlvo.Proposta && s.RegistroId == propostaId)
                .OrderByDescending(s => s.Id).FirstOrDefaultAsync(ct);
            if (aprovar && p.StatusAprovacao == StatusAprovacaoProposta.Aprovada && ultima is { Status: StatusSolicitacaoAprovacao.Aprovado })
                throw new RegraNegocioException("A proposta já está aprovada.");
            if (!aprovar && p.StatusAprovacao == StatusAprovacaoProposta.Rejeitada && ultima is { Status: StatusSolicitacaoAprovacao.Rejeitado })
                throw new RegraNegocioException("A proposta já está rejeitada.");
            ativa = await AvaliarInternoAsync(p, explicito: true, null, ct);
            if (ativa is null) return false;
        }
        await DecidirInternoAsync(ativa, p, aprovar, comentario, ct);
        return true;
    }

    public async Task<SolicitacaoAprovacaoDto> DecidirSolicitacaoAsync(long solicitacaoId, bool aprovar, string? comentario, CancellationToken ct = default)
    {
        var t = db.EmpresaIdAtual;
        var sol = await db.SolicitacoesAprovacao.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId && s.EmpresaId == t, ct)
            ?? throw new KeyNotFoundException($"Solicitação {solicitacaoId} não encontrada.");
        if (!sol.Ativo) throw new RegraNegocioException("Esta solicitação foi cancelada.");
        if (sol.EntidadeAlvo != EntidadeAlvo.Proposta) throw new RegraNegocioException("Só há aprovação de propostas.");
        var p = await db.Propostas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == sol.RegistroId && x.EmpresaId == t, ct)
            ?? throw new RegraNegocioException("A proposta desta solicitação não existe mais.");
        await DecidirInternoAsync(sol, p, aprovar, comentario, ct);
        return (await MontarAsync([sol], ct))[0];
    }

    private async Task DecidirInternoAsync(SolicitacaoAprovacao sol, Proposta p, bool aprovar, string? comentario, CancellationToken ct)
    {
        if (sol.Status is StatusSolicitacaoAprovacao.Aprovado or StatusSolicitacaoAprovacao.Rejeitado)
            throw new RegraNegocioException("Esta solicitação já foi concluída.");

        comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
        if (comentario is { Length: > 1000 }) throw new RegraNegocioException("O comentário pode ter até 1000 caracteres.");

        var fluxos = await CarregarFluxosAsync([sol.FluxoAprovacaoId], ct);
        if (!fluxos.TryGetValue(sol.FluxoAprovacaoId, out var fluxo))
            throw new RegraNegocioException("O fluxo desta solicitação não existe mais.");
        var nivel = fluxo.Niveis.FirstOrDefault(n => n.Nivel == sol.NivelAtual);
        var total = fluxo.Niveis.Count;

        var eu = db.UsuarioIdAtual ?? throw new AcessoNegadoException("Somente um usuário autenticado pode aprovar ou rejeitar.");
        var ehAprovador = nivel is not null && EhAprovador(nivel, eu, db.PerfilAtual);
        var foraDaAlcada = false;
        if (!ehAprovador)
        {
            if (db.PerfilAtual != PerfilUsuario.Administrador)
                throw new AcessoNegadoException($"Você não é aprovador do nível {sol.NivelAtual} desta solicitação.");
            foraDaAlcada = true;
        }

        if (!aprovar && comentario is null)
            throw new RegraNegocioException("Informe o motivo da rejeição no comentário.");

        var agora = DateTime.UtcNow;
        var nivelDecidido = sol.NivelAtual;
        sol.AprovadoPorId = eu;
        sol.ComentarioDecisao = comentario;

        string tituloLog;
        if (!aprovar)
        {
            sol.Status = StatusSolicitacaoAprovacao.Rejeitado;
            sol.RejeitadoEm = agora;
            p.StatusAprovacao = StatusAprovacaoProposta.Rejeitada;
            p.AprovadorId = eu;
            tituloLog = $"Nível {nivelDecidido} rejeitado";
        }
        else if (nivelDecidido >= total)
        {
            sol.Status = StatusSolicitacaoAprovacao.Aprovado;
            sol.AprovadoEm = agora;
            p.StatusAprovacao = StatusAprovacaoProposta.Aprovada;
            p.AprovadorId = eu;
            tituloLog = $"Nível {nivelDecidido} aprovado (aprovação concluída)";
        }
        else
        {
            sol.Status = StatusSolicitacaoAprovacao.AprovadoNivel;
            sol.NivelAtual = nivelDecidido + 1;
            tituloLog = $"Nível {nivelDecidido} aprovado";
        }
        if (foraDaAlcada) tituloLog += " por Administrador (fora da alçada do nível)";

        db.RegistrosAlteracao.Add(new RegistroAlteracao
        {
            EmpresaId = sol.EmpresaId,
            UsuarioId = eu,
            UsuarioNome = db.UsuarioNomeAtual,
            Usuario = db.UsuarioAtual,
            Acao = AcaoDecisao,
            EntidadeAlvo = EntidadeAlvo.Proposta,
            RegistroId = p.Id,
            Titulo = tituloLog,
            DadosDepois = JsonSerializer.Serialize(new
            {
                solicitacaoId = sol.Id,
                nivel = nivelDecidido,
                decisao = aprovar ? "Aprovado" : "Rejeitado",
                comentario,
                foraDaAlcada
            })
        });
        await db.SaveChangesAsync(ct);

        if (sol.Status == StatusSolicitacaoAprovacao.AprovadoNivel) return;
        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = p.EmpresaId,
            RegistroId = p.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = aprovar ? "PropostaAprovada" : "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    private static bool EhAprovador(NivelAprovacao nivel, long usuarioId, PerfilUsuario? perfil)
        => nivel.Aprovadores.Any(a => a.UsuarioId == usuarioId || (a.UsuarioId is null && a.Perfil != null && a.Perfil == perfil));

    // =====================================================================
    // Consultas
    // =====================================================================

    // List (e não array): o Contains de array vira span no C# 14 e o EF não consegue traduzir.
    private static readonly List<StatusSolicitacaoAprovacao> EmAndamentoStatus =
        [StatusSolicitacaoAprovacao.Pendente, StatusSolicitacaoAprovacao.AprovadoNivel];

    public async Task<List<SolicitacaoAprovacaoDto>> MinhasAsync(CancellationToken ct = default)
    {
        var eu = db.UsuarioIdAtual;
        if (eu is null) return [];
        var abertas = await db.SolicitacoesAprovacao.AsNoTracking()
            .Where(s => s.EntidadeAlvo == EntidadeAlvo.Proposta && EmAndamentoStatus.Contains(s.Status))
            .OrderByDescending(s => s.Id).Take(300).ToListAsync(ct);
        var fluxos = await CarregarFluxosAsync(abertas.Select(s => s.FluxoAprovacaoId), ct);
        var minhas = abertas.Where(s => fluxos.TryGetValue(s.FluxoAprovacaoId, out var f)
            && f.Niveis.FirstOrDefault(n => n.Nivel == s.NivelAtual) is { } n && EhAprovador(n, eu.Value, db.PerfilAtual))
            .Take(100).ToList();
        return await MontarAsync(minhas, ct);
    }

    public async Task<List<SolicitacaoAprovacaoDto>> SolicitadasPorMimAsync(CancellationToken ct = default)
    {
        var eu = db.UsuarioIdAtual;
        if (eu is null) return [];
        var t = db.EmpresaIdAtual;
        var sols = await db.SolicitacoesAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.EmpresaId == t && s.EntidadeAlvo == EntidadeAlvo.Proposta && s.SolicitadoPorId == eu)
            .OrderByDescending(s => s.Id).Take(100).ToListAsync(ct);
        return await MontarAsync(sols, ct);
    }

    public async Task<List<SolicitacaoAprovacaoDto>> HistoricoAsync(string? entidade, long? id, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(entidade) && !entidade.Trim().Equals("Proposta", StringComparison.OrdinalIgnoreCase))
            throw new RegraNegocioException("Só há aprovação de propostas (entidade=Proposta).");

        var t = db.EmpresaIdAtual;
        var q = db.SolicitacoesAprovacao.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.EmpresaId == t && s.EntidadeAlvo == EntidadeAlvo.Proposta);
        if (id is { } rid)
            q = q.Where(s => s.RegistroId == rid);
        else
        {
            // Sem registro: o que já foi concluído ou cancelado; Administrador vê tudo, os demais o que pediram ou decidiram.
            q = q.Where(s => !EmAndamentoStatus.Contains(s.Status) || !s.Ativo);
            if (db.PerfilAtual != PerfilUsuario.Administrador)
            {
                var eu = db.UsuarioIdAtual;
                q = q.Where(s => s.SolicitadoPorId == eu || s.AprovadoPorId == eu);
            }
        }
        var sols = await q.OrderByDescending(s => s.Id).Take(100).ToListAsync(ct);
        return await MontarAsync(sols, ct);
    }

    // ---------- Montagem dos DTOs ----------

    private async Task<List<SolicitacaoAprovacaoDto>> MontarAsync(List<SolicitacaoAprovacao> sols, CancellationToken ct)
    {
        if (sols.Count == 0) return [];
        var t = db.EmpresaIdAtual;
        var fluxos = await CarregarFluxosAsync(sols.Select(s => s.FluxoAprovacaoId), ct);

        var propostaIds = sols.Select(s => s.RegistroId).Distinct().ToList();
        var propostas = await db.Propostas.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.EmpresaId == t && propostaIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var itens = (await db.ItensProposta.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.EmpresaId == t && i.Ativo && propostaIds.Contains(i.PropostaId)).ToListAsync(ct))
            .ToLookup(i => i.PropostaId);

        var clienteIds = propostas.Values.Where(p => p.ClienteId != null).Select(p => p.ClienteId!.Value).Distinct().ToList();
        var clientes = clienteIds.Count == 0 ? [] : await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.EmpresaId == t && clienteIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var moedaIds = propostas.Values.Where(p => p.MoedaId != null).Select(p => p.MoedaId!.Value).Distinct().ToList();
        var moedas = moedaIds.Count == 0 ? [] : await db.Moedas.IgnoreQueryFilters().AsNoTracking()
            .Where(m => moedaIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);

        var usuarioIds = sols.SelectMany(s => new[] { s.SolicitadoPorId, s.AprovadoPorId }).Where(x => x != null).Select(x => x!.Value)
            .Concat(fluxos.Values.SelectMany(f => f.Niveis).SelectMany(n => n.Aprovadores).Where(a => a.UsuarioId != null).Select(a => a.UsuarioId!.Value));
        var nomes = await NomesUsuariosAsync(usuarioIds, ct);

        // Decisões por nível (gravadas no log de alterações)
        var logs = await db.RegistrosAlteracao.AsNoTracking()
            .Where(r => r.EmpresaId == t && r.EntidadeAlvo == EntidadeAlvo.Proposta && r.Acao == AcaoDecisao && propostaIds.Contains(r.RegistroId))
            .OrderBy(r => r.Id).ToListAsync(ct);
        var decisoes = new List<(long SolicitacaoId, int Nivel, string Decisao, string? Comentario, bool Fora, RegistroAlteracao Log)>();
        foreach (var l in logs)
        {
            try
            {
                using var doc = JsonDocument.Parse(l.DadosDepois ?? "{}");
                var r = doc.RootElement;
                decisoes.Add((r.GetProperty("solicitacaoId").GetInt64(), r.GetProperty("nivel").GetInt32(),
                    r.GetProperty("decisao").GetString() ?? "",
                    r.TryGetProperty("comentario", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null,
                    r.TryGetProperty("foraDaAlcada", out var fa) && fa.ValueKind == JsonValueKind.True, l));
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { }
        }

        var eu = db.UsuarioIdAtual;
        var lista = new List<SolicitacaoAprovacaoDto>();
        foreach (var s in sols)
        {
            fluxos.TryGetValue(s.FluxoAprovacaoId, out var fluxo);
            var emAndamento = EmAndamentoStatus.Contains(s.Status);
            var situacao = !s.Ativo && emAndamento ? "Cancelada" : s.Status switch
            {
                StatusSolicitacaoAprovacao.Aprovado => "Aprovada",
                StatusSolicitacaoAprovacao.Rejeitado => "Rejeitada",
                _ => "EmAndamento"
            };
            var niveis = fluxo?.Niveis ?? [];

            var passos = new List<PassoAprovacaoDto>();
            foreach (var n in niveis)
            {
                var passo = new PassoAprovacaoDto
                {
                    Nivel = n.Nivel,
                    Descricao = n.Descricao,
                    Aprovadores = n.Aprovadores.Select(a => a.UsuarioId is { } u
                        ? (nomes.TryGetValue(u, out var nome) ? nome : $"Usuário {u}")
                        : $"Qualquer {a.Perfil}").ToList()
                };
                var d = decisoes.LastOrDefault(x => x.SolicitacaoId == s.Id && x.Nivel == n.Nivel);
                if (d.Log is not null)
                {
                    passo.Estado = d.Decisao;
                    passo.DecididoPorId = d.Log.UsuarioId;
                    passo.DecididoPorNome = d.Log.UsuarioNome ?? d.Log.Usuario;
                    passo.DecididoEm = d.Log.DataHora;
                    passo.Comentario = d.Comentario;
                    passo.ForaDaAlcada = d.Fora;
                }
                else if (situacao == "Cancelada" && n.Nivel >= s.NivelAtual) passo.Estado = "Cancelado";
                else if (situacao == "EmAndamento" && n.Nivel == s.NivelAtual) passo.Estado = "Atual";
                passos.Add(passo);
            }

            var podeDecidir = false;
            if (situacao == "EmAndamento" && eu is { } uid)
            {
                var atual = niveis.FirstOrDefault(n => n.Nivel == s.NivelAtual);
                podeDecidir = db.PerfilAtual == PerfilUsuario.Administrador || (atual is not null && EhAprovador(atual, uid, db.PerfilAtual));
            }

            PropostaResumoAprovacaoDto? resumo = null;
            if (propostas.TryGetValue(s.RegistroId, out var prop))
            {
                var m = Metricas(prop, itens[prop.Id].ToList());
                moedas.TryGetValue(prop.MoedaId ?? 0, out var moeda);
                resumo = new PropostaResumoAprovacaoDto
                {
                    Id = prop.Id,
                    Numero = prop.Numero,
                    Titulo = prop.Titulo,
                    ClienteId = prop.ClienteId,
                    ClienteNome = prop.ClienteId is { } cid && clientes.TryGetValue(cid, out var cn) ? cn : null,
                    Valor = prop.Valor ?? m.Valor,
                    MoedaId = prop.MoedaId,
                    MoedaSimbolo = moeda?.Simbolo,
                    MoedaCodigo = moeda?.Codigo,
                    DescontoPercentual = m.DescontoPercentual,
                    StatusAprovacao = prop.StatusAprovacao?.ToString()
                };
            }

            var dec = situacao is "Aprovada" or "Rejeitada";
            lista.Add(new SolicitacaoAprovacaoDto
            {
                Id = s.Id,
                FluxoId = s.FluxoAprovacaoId,
                FluxoNome = fluxo?.Nome,
                Entidade = "Proposta",
                RegistroId = s.RegistroId,
                Status = s.Status.ToString(),
                Situacao = situacao,
                NivelAtual = s.NivelAtual,
                TotalNiveis = niveis.Count,
                SolicitadoPorId = s.SolicitadoPorId,
                SolicitadoPorNome = s.SolicitadoPorId is { } sp && nomes.TryGetValue(sp, out var spn) ? spn : null,
                SolicitadoEm = s.CriadoEm,
                Observacao = s.Observacao,
                DecididoPorId = s.AprovadoPorId,
                DecididoPorNome = s.AprovadoPorId is { } ap && nomes.TryGetValue(ap, out var apn) ? apn : null,
                DecididoEm = dec ? (s.AprovadoEm ?? s.RejeitadoEm) : null,
                ComentarioDecisao = s.ComentarioDecisao,
                PodeDecidir = podeDecidir,
                Proposta = resumo,
                Passos = passos
            });
        }
        return lista;
    }

    private static string? Truncar(string? s, int max) => s is null || s.Length <= max ? s : s[..max];
}

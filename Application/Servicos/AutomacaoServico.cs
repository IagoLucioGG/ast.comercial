using System.Text.Json;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Automacoes;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

/// <summary>
/// Semântica de Ativo em Automacao: o motor e o publicador usam <c>Ativo</c> como "habilitada".
/// A exclusão lógica usa a coluna própria <c>Excluida</c> (e também deixa Ativo=false, que é o que o motor/agendador enxergam).
/// Por isso as consultas daqui usam IgnoreQueryFilters com filtro explícito de empresa.
/// </summary>
public class AutomacaoServico(AppDbContext db) : IAutomacaoServico
{
    private IQueryable<Automacao> Base()
        => db.Automacoes.IgnoreQueryFilters()
            .Where(a => a.EmpresaId == db.EmpresaIdAtual && !a.Excluida);

    // ---------------- OData (leitura) ----------------

    public IQueryable<Automacao> ObterTodos() => Base().AsNoTracking();

    public async Task<Automacao?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await Base().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public IQueryable<FilaAutomacao> ObterFila()
        => db.FilaAutomacao.AsNoTracking().Where(f => f.EmpresaId == db.EmpresaIdAtual);

    public IQueryable<ExecucaoAutomacao> ObterLogs()
        => db.ExecucoesAutomacao.AsNoTracking().Where(e => e.EmpresaId == db.EmpresaIdAtual);

    // ---------------- API ----------------

    public async Task<ListaPaginadaDto<AutomacaoDto>> ListarAsync(string? entidadeAlvo, string? gatilho, bool? ativo, string? busca, int skip, int top, CancellationToken ct = default)
    {
        var q = Base().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entidadeAlvo))
        {
            var e = ParseEnum<EntidadeAlvo>(entidadeAlvo);
            if (e is null) throw new ValidacaoAutomacaoException([$"entidadeAlvo '{entidadeAlvo}' inválida."]);
            q = q.Where(a => a.EntidadeAlvo == e.Value);
        }
        if (!string.IsNullOrWhiteSpace(gatilho))
        {
            var g = ParseEnum<TipoGatilho>(gatilho);
            if (g is null) throw new ValidacaoAutomacaoException([$"gatilho '{gatilho}' inválido."]);
            q = q.Where(a => a.Gatilho == g.Value);
        }
        if (ativo is not null) q = q.Where(a => a.Ativo == ativo.Value);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            q = q.Where(a => EF.Functions.ILike(a.Nome, termo));
        }

        top = Math.Clamp(top, 1, 200);
        skip = Math.Max(0, skip);

        var total = await q.CountAsync(ct);
        var itens = await q.OrderBy(a => a.Ordem).ThenBy(a => a.Id).Skip(skip).Take(top)
            .Include(a => a.CriadoPor)
            .Include(a => a.Filtros.Where(f => f.Ativo).OrderBy(f => f.Grupo).ThenBy(f => f.Ordem))
            .Include(a => a.Acoes.Where(x => x.Ativo).OrderBy(x => x.Ordem))
            .AsSplitQuery()
            .ToListAsync(ct);

        return new ListaPaginadaDto<AutomacaoDto> { Total = total, Itens = itens.Select(ParaDto).ToList() };
    }

    public async Task<AutomacaoDto?> ObterDtoAsync(long id, CancellationToken ct = default)
    {
        var a = await Base().AsNoTracking()
            .Include(x => x.CriadoPor)
            .Include(x => x.Filtros.Where(f => f.Ativo).OrderBy(f => f.Grupo).ThenBy(f => f.Ordem))
            .Include(x => x.Acoes.Where(ac => ac.Ativo).OrderBy(ac => ac.Ordem))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? null : ParaDto(a);
    }

    public async Task<AutomacaoDto> CriarAsync(AutomacaoEntradaDto entrada, CancellationToken ct = default)
    {
        var v = await ValidarAsync(entrada, ct);
        var a = new Automacao { EmpresaId = db.EmpresaIdAtual, Ativo = entrada.Ativo ?? true, CriadoPorUsuarioId = db.UsuarioIdAtual };
        Aplicar(a, entrada, v);
        db.Automacoes.Add(a);
        await db.SaveChangesAsync(ct);
        return (await ObterDtoAsync(a.Id, ct))!;
    }

    public async Task<AutomacaoDto?> AtualizarAsync(long id, AutomacaoEntradaDto entrada, CancellationToken ct = default)
    {
        var a = await Base().Include(x => x.Filtros).Include(x => x.Acoes).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;

        var v = await ValidarAsync(entrada, ct);

        // Substituição conjunta: remove tudo (inclusive resíduos inativos) e recria na ordem recebida
        db.FiltrosAutomacao.RemoveRange(a.Filtros);
        db.AcoesAutomacao.RemoveRange(a.Acoes);
        a.Filtros = [];
        a.Acoes = [];

        if (entrada.Ativo is not null) a.Ativo = entrada.Ativo.Value;
        Aplicar(a, entrada, v);
        await db.SaveChangesAsync(ct);
        return await ObterDtoAsync(a.Id, ct);
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var a = await Base().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return false;

        a.Ativo = false;
        a.Excluida = true;
        a.ProximaExecucao = null;
        await CancelarPendentesAsync(a.Id, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AutomacaoDto?> DefinirAtivoAsync(long id, bool ativo, CancellationToken ct = default)
    {
        var a = await Base().Include(x => x.Acoes).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;

        if (ativo)
        {
            var erros = new List<string>();
            var def = CatalogoAutomacao.Gatilhos.First(g => g.Valor == a.Gatilho);
            if (!def.Implementado) erros.Add($"O gatilho {a.Gatilho} não está implementado.");
            foreach (var ac in a.Acoes.Where(x => x.Ativo))
                if (!CatalogoAutomacao.Acoes.First(d => d.Valor == ac.Tipo).Implementado)
                    erros.Add($"A ação {ac.Tipo} não está implementada.");
            if (!a.Acoes.Any(x => x.Ativo)) erros.Add("A automação não possui ações.");
            if (erros.Count > 0) throw new ValidacaoAutomacaoException(erros);

            if (a.Gatilho == TipoGatilho.Periodicamente && a.HorarioExecucao is not null)
                a.ProximaExecucao = ProximoHorario(a.HorarioExecucao.Value);
        }
        else
        {
            a.ProximaExecucao = null;
            await CancelarPendentesAsync(a.Id, ct);
        }

        a.Ativo = ativo;
        await db.SaveChangesAsync(ct);
        return await ObterDtoAsync(a.Id, ct);
    }

    public async Task<ListaPaginadaDto<ExecucaoAutomacaoDto>?> ListarExecucoesAsync(long id, string? status, int skip, int top, CancellationToken ct = default)
    {
        if (!await Base().AnyAsync(a => a.Id == id, ct)) return null;

        var q = db.ExecucoesAutomacao.AsNoTracking()
            .Where(e => e.EmpresaId == db.EmpresaIdAtual && e.AutomacaoId == id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = ParseEnum<StatusExecucao>(status);
            if (s is null) throw new ValidacaoAutomacaoException([$"status '{status}' inválido."]);
            q = q.Where(e => e.Status == s.Value);
        }

        top = Math.Clamp(top, 1, 200);
        skip = Math.Max(0, skip);
        var total = await q.CountAsync(ct);
        var itens = await q.OrderByDescending(e => e.IniciadaEm).ThenByDescending(e => e.Id).Skip(skip).Take(top)
            .Select(e => new ExecucaoAutomacaoDto
            {
                Id = e.Id, AutomacaoId = e.AutomacaoId, RegistroId = e.RegistroId,
                EntidadeAlvo = e.EntidadeAlvo.ToString(), Status = e.Status.ToString(),
                IniciadaEm = e.IniciadaEm, FinalizadaEm = e.FinalizadaEm, DuracaoMs = e.DuracaoMs,
                Detalhes = e.Detalhes, Erro = e.Erro, DisparadoPor = e.DisparadoPor
            })
            .ToListAsync(ct);

        return new ListaPaginadaDto<ExecucaoAutomacaoDto> { Total = total, Itens = itens };
    }

    // ---------------- Internos ----------------

    private async Task CancelarPendentesAsync(long automacaoId, CancellationToken ct)
    {
        var pendentes = await db.FilaAutomacao
            .Where(f => f.AutomacaoId == automacaoId && f.EmpresaId == db.EmpresaIdAtual
                && (f.Status == StatusFila.Pendente || f.Status == StatusFila.Falha))
            .ToListAsync(ct);
        foreach (var f in pendentes)
        {
            f.Status = StatusFila.Cancelado;
            f.FinalizadoEm = DateTime.UtcNow;
        }
    }

    private record Validado(EntidadeAlvo Entidade, TipoGatilho Gatilho, TimeOnly? Horario,
        List<(int Grupo, string Campo, OperadorFiltro Op, string? Valor)> Filtros,
        List<(TipoAcao Tipo, string Json)> Acoes);

    private async Task<Validado> ValidarAsync(AutomacaoEntradaDto e, CancellationToken ct)
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(e.Nome)) erros.Add("O nome é obrigatório.");
        else if (e.Nome.Trim().Length > 200) erros.Add("O nome excede 200 caracteres.");
        if (e.Descricao is { Length: > 1000 }) erros.Add("A descrição excede 1000 caracteres.");
        if (!string.IsNullOrWhiteSpace(e.CronExpressao)) erros.Add("cronExpressao não é suportado pelo motor; use horarioExecucao.");
        if (!string.IsNullOrWhiteSpace(e.FormulaCondicao)) erros.Add("formulaCondicao não é suportada pelo motor; use filtros.");
        if (!string.IsNullOrWhiteSpace(e.GatilhoCampoReferencia)) erros.Add("gatilhoCampoReferencia não é suportado (gatilho CampoAlterado não implementado).");

        var entidade = ParseEnum<EntidadeAlvo>(e.EntidadeAlvo);
        if (entidade is null || !CatalogoAutomacao.EntidadesAlvo.Contains(entidade.Value))
            erros.Add($"entidadeAlvo inválida. Use: {string.Join(", ", CatalogoAutomacao.EntidadesAlvo)}.");
        var gatilho = ParseEnum<TipoGatilho>(e.Gatilho);
        if (gatilho is null) erros.Add($"gatilho inválido. Use: {string.Join(", ", CatalogoAutomacao.Gatilhos.Where(g => g.Implementado).Select(g => g.Valor))}.");

        TimeOnly? horario = null;
        if (entidade is not null && gatilho is not null)
        {
            var def = CatalogoAutomacao.Gatilhos.First(g => g.Valor == gatilho.Value);
            if (!def.Implementado)
                erros.Add($"O gatilho {gatilho} ainda não está implementado (nenhum evento o dispara).");
            else if (!def.Entidades.Contains(entidade.Value))
                erros.Add($"O gatilho {gatilho} não é compatível com a entidade {entidade}. Entidades válidas: {string.Join(", ", def.Entidades)}.");
        }

        if (gatilho == TipoGatilho.Periodicamente)
        {
            if (!TimeOnly.TryParseExact(e.HorarioExecucao, ["HH:mm", "HH:mm:ss"], null, System.Globalization.DateTimeStyles.None, out var h))
                erros.Add("Gatilho Periodicamente exige horarioExecucao no formato HH:mm (UTC).");
            else horario = h;
        }
        else if (!string.IsNullOrWhiteSpace(e.HorarioExecucao))
            erros.Add("horarioExecucao só é permitido no gatilho Periodicamente.");

        // Filtros
        var filtros = new List<(int, string, OperadorFiltro, string?)>();
        var camposEntidade = entidade is null ? [] : CatalogoAutomacao.Campos(entidade.Value).Select(c => c.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var listaFiltros = e.Filtros ?? [];
        if (listaFiltros.Count > 50) erros.Add("No máximo 50 filtros.");
        for (var i = 0; i < listaFiltros.Count && i < 50; i++)
        {
            var f = listaFiltros[i];
            var p = $"Filtro {i + 1}";
            if (f is null) { erros.Add($"{p}: inválido."); continue; }
            if (f.Grupo is < 0 or > 100) erros.Add($"{p}: grupo deve estar entre 0 e 100.");
            var campo = f.CampoReferencia?.Trim() ?? "";
            if (!CatalogoAutomacao.RegexCampo().IsMatch(campo))
                erros.Add($"{p}: campoReferencia inválido (use o nome do campo, sem navegação).");
            else if (entidade is not null && !camposEntidade.Contains(campo))
                erros.Add($"{p}: o campo '{campo}' não existe em {entidade}.");
            var op = ParseEnum<OperadorFiltro>(f.Operador);
            if (op is null) { erros.Add($"{p}: operador inválido."); continue; }
            var defOp = CatalogoAutomacao.Operadores.First(o => o.Valor == op.Value);
            if (!defOp.Implementado) erros.Add($"{p}: o operador {op} não é suportado pelo motor.");
            if (defOp.PrecisaValor && string.IsNullOrEmpty(f.Valor)) erros.Add($"{p}: o operador {op} exige 'valor'.");
            if (f.Valor is { Length: > 2000 }) erros.Add($"{p}: valor excede 2000 caracteres.");
            filtros.Add((f.Grupo, campo, op.Value, defOp.PrecisaValor ? f.Valor : null));
        }

        // Ações
        var acoes = new List<(TipoAcao, string)>();
        var listaAcoes = e.Acoes ?? [];
        if (listaAcoes.Count == 0) erros.Add("Informe ao menos uma ação.");
        if (listaAcoes.Count > 20) erros.Add("No máximo 20 ações.");
        for (var i = 0; i < listaAcoes.Count && i < 20; i++)
        {
            var a = listaAcoes[i];
            var p = $"Ação {i + 1}";
            if (a is null) { erros.Add($"{p}: inválida."); continue; }
            var tipo = ParseEnum<TipoAcao>(a.Tipo);
            if (tipo is null) { erros.Add($"{p}: tipo inválido."); continue; }
            var def = CatalogoAutomacao.Acoes.First(d => d.Valor == tipo.Value);
            if (!def.Implementado) { erros.Add($"{p}: a ação {tipo} ainda não está implementada."); continue; }
            if (entidade is not null)
            {
                if (def.Entidades is not null && !def.Entidades.Contains(entidade.Value))
                    erros.Add($"{p}: a ação {tipo} não é válida para a entidade {entidade}. Válida para: {string.Join(", ", def.Entidades)}.");
                if (gatilho == TipoGatilho.Periodicamente && !CatalogoAutomacao.AcoesSemRegistro.Contains(tipo.Value))
                    erros.Add($"{p}: no gatilho Periodicamente (sem registro) só são permitidas: {string.Join(", ", CatalogoAutomacao.AcoesSemRegistro)}.");
            }
            var errosCfg = CatalogoAutomacao.ValidarConfiguracao(tipo.Value, entidade ?? EntidadeAlvo.Cliente, a.Configuracao, p);
            erros.AddRange(errosCfg);
            if (errosCfg.Count == 0)
                erros.AddRange(await ValidarReferenciasAsync(tipo.Value, a.Configuracao, p, ct));
            acoes.Add((tipo.Value, a.Configuracao is { ValueKind: JsonValueKind.Object } cfg ? cfg.GetRawText() : "{}"));
        }

        if (erros.Count > 0) throw new ValidacaoAutomacaoException(erros);
        return new Validado(entidade!.Value, gatilho!.Value, horario, filtros, acoes);
    }

    private async Task<List<string>> ValidarReferenciasAsync(TipoAcao tipo, JsonElement? config, string prefixo, CancellationToken ct)
    {
        var erros = new List<string>();
        if (config is not { ValueKind: JsonValueKind.Object } c) return erros;
        switch (tipo)
        {
            case TipoAcao.AlterarStatus:
            {
                var id = CatalogoAutomacao.LerId(c, "statusId");
                if (!await db.StatusNegocios.AnyAsync(s => s.Id == id, ct)) erros.Add($"{prefixo}: statusId {id} não existe nesta empresa.");
                break;
            }
            case TipoAcao.MoverEtapa:
            {
                var id = CatalogoAutomacao.LerId(c, "etapaId");
                if (!await db.EtapasFunil.AnyAsync(s => s.Id == id, ct)) erros.Add($"{prefixo}: etapaId {id} não existe nesta empresa.");
                break;
            }
            case TipoAcao.AdicionarEtiqueta:
            case TipoAcao.RemoverEtiqueta:
            {
                var id = CatalogoAutomacao.LerId(c, "etiquetaId");
                if (!await db.Etiquetas.AnyAsync(s => s.Id == id, ct)) erros.Add($"{prefixo}: etiquetaId {id} não existe nesta empresa.");
                break;
            }
        }
        return erros;
    }

    private static void Aplicar(Automacao a, AutomacaoEntradaDto e, Validado v)
    {
        a.Nome = e.Nome!.Trim();
        a.Descricao = string.IsNullOrWhiteSpace(e.Descricao) ? null : e.Descricao.Trim();
        a.EntidadeAlvo = v.Entidade;
        a.Gatilho = v.Gatilho;
        a.Ordem = e.Ordem;
        a.ImpedirCascata = e.ImpedirCascata;
        a.CronExpressao = null;
        a.FormulaCondicao = null;
        a.GatilhoCampoReferencia = null;
        a.HorarioExecucao = v.Horario;
        a.ProximaExecucao = v.Gatilho == TipoGatilho.Periodicamente && a.Ativo && v.Horario is not null
            ? ProximoHorario(v.Horario.Value) : null;

        var ordemFiltro = 0;
        foreach (var f in v.Filtros)
            a.Filtros.Add(new FiltroAutomacao
            {
                EmpresaId = a.EmpresaId, Grupo = f.Grupo, Ordem = ordemFiltro++,
                CampoReferencia = f.Campo, Operador = f.Op, Valor = f.Valor
            });

        var ordemAcao = 0;
        foreach (var ac in v.Acoes)
            a.Acoes.Add(new AcaoAutomacao { EmpresaId = a.EmpresaId, Ordem = ordemAcao++, Tipo = ac.Tipo, Configuracao = ac.Json });
    }

    private static DateTime ProximoHorario(TimeOnly horario)
    {
        var agora = DateTime.UtcNow;
        var hoje = agora.Date.Add(horario.ToTimeSpan());
        return hoje > agora ? hoje : hoje.AddDays(1);
    }

    private static AutomacaoDto ParaDto(Automacao a) => new()
    {
        Id = a.Id, Nome = a.Nome, Descricao = a.Descricao,
        EntidadeAlvo = a.EntidadeAlvo.ToString(), Gatilho = a.Gatilho.ToString(),
        Ordem = a.Ordem, ImpedirCascata = a.ImpedirCascata, Ativo = a.Ativo,
        HorarioExecucao = a.HorarioExecucao?.ToString("HH:mm"),
        ProximaExecucao = a.ProximaExecucao, UltimaExecucao = a.UltimaExecucao,
        CriadoEm = a.CriadoEm, AtualizadoEm = a.AtualizadoEm,
        CriadoPorUsuarioId = a.CriadoPorUsuarioId, CriadoPor = a.CriadoPor?.Nome,
        Filtros = a.Filtros.Where(f => f.Ativo).OrderBy(f => f.Grupo).ThenBy(f => f.Ordem).Select(f => new FiltroAutomacaoDto
        {
            Id = f.Id, Grupo = f.Grupo, Ordem = f.Ordem, CampoReferencia = f.CampoReferencia,
            Operador = f.Operador.ToString(), Valor = f.Valor
        }).ToList(),
        Acoes = a.Acoes.Where(x => x.Ativo).OrderBy(x => x.Ordem).Select(x => new AcaoAutomacaoDto
        {
            Id = x.Id, Ordem = x.Ordem, Tipo = x.Tipo.ToString(), Configuracao = LerJson(x.Configuracao)
        }).ToList()
    };

    private static JsonElement LerJson(string? json)
    {
        try { using var d = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json); return d.RootElement.Clone(); }
        catch (JsonException) { using var d = JsonDocument.Parse("{}"); return d.RootElement.Clone(); }
    }

    /// <summary>Aceita apenas o nome do enum (ignora maiúsculas); números são rejeitados.</summary>
    private static T? ParseEnum<T>(string? valor) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        foreach (var nome in Enum.GetNames<T>())
            if (string.Equals(nome, valor.Trim(), StringComparison.OrdinalIgnoreCase))
                return Enum.Parse<T>(nome);
        return null;
    }
}

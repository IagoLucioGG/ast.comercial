using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class MetaServico(AppDbContext db) : IMetaServico
{
    private const int MaxDiasSerie = 800;

    private sealed record Linha(long? Responsavel, DateTime Data, decimal Valor);

    public IQueryable<Meta> ObterTodos() => db.Metas.AsNoTracking();

    public async Task<Meta?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Metas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Meta> CriarAsync(Delta<Meta> delta, CancellationToken ct = default)
    {
        var meta = new Meta();
        delta.Patch(meta);
        meta.Id = 0;
        meta.EmpresaId = db.EmpresaIdAtual;
        meta.Ativo = true;
        await NormalizarEValidarAsync(meta, ct);
        db.Metas.Add(meta);
        await db.SaveChangesAsync(ct);
        return meta;
    }

    public async Task<Meta?> AtualizarAsync(long id, Delta<Meta> delta, CancellationToken ct = default)
    {
        var meta = await db.Metas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (meta is null) return null;
        var empresaId = meta.EmpresaId;
        delta.Patch(meta);
        meta.Id = id;
        meta.EmpresaId = empresaId;
        meta.Ativo = true;
        await NormalizarEValidarAsync(meta, ct);
        meta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return meta;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var meta = await db.Metas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (meta is null) return false;
        meta.Ativo = false;
        meta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static DateTime Utc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc),
    };

    private async Task NormalizarEValidarAsync(Meta m, CancellationToken ct)
    {
        m.Nome = (m.Nome ?? string.Empty).Trim();
        if (m.Nome.Length == 0) throw new RegraNegocioException("Informe o nome da meta.");
        if (m.Nome.Length > 200) throw new RegraNegocioException("O nome da meta deve ter no máximo 200 caracteres.");
        if (!Enum.IsDefined(m.Metrica)) throw new RegraNegocioException("Métrica inválida.");
        if (m.Alvo <= 0) throw new RegraNegocioException("O alvo deve ser maior que zero.");
        if (m.Inicio == default || m.Fim == default) throw new RegraNegocioException("Informe o início e o fim da meta.");
        m.Inicio = Utc(m.Inicio);
        m.Fim = Utc(m.Fim);
        if (m.Fim <= m.Inicio) throw new RegraNegocioException("O fim da meta deve ser posterior ao início.");
        if (m.UsuarioId is not null && m.EquipeId is not null)
            throw new RegraNegocioException("Defina a meta para um usuário, uma equipe ou a empresa inteira (não usuário e equipe ao mesmo tempo).");

        if (m.Metrica == MetricaMeta.ValorGanho)
        {
            if (m.MoedaId is null) throw new RegraNegocioException("Informe a moeda: a métrica Valor ganho é monetária.");
            if (!await db.Moedas.IgnoreQueryFilters().AnyAsync(x => x.Id == m.MoedaId && x.Ativo && (x.EmpresaId == 0 || x.EmpresaId == db.EmpresaIdAtual), ct)) throw new RegraNegocioException("Moeda não encontrada.");
        }
        else
        {
            m.MoedaId = null; // métricas de contagem não usam moeda
        }
        if (m.UsuarioId is not null && !await db.Usuarios.AnyAsync(x => x.Id == m.UsuarioId, ct))
            throw new RegraNegocioException("Usuário não encontrado.");
        if (m.EquipeId is not null && !await db.Equipes.AnyAsync(x => x.Id == m.EquipeId, ct))
            throw new RegraNegocioException("Equipe não encontrada.");
        if (m.FunilId is not null && !await db.Funis.AnyAsync(x => x.Id == m.FunilId, ct))
            throw new RegraNegocioException("Funil não encontrado.");
    }

    // ------------------------------------------------------------------ progresso

    public async Task<IReadOnlyList<ProgressoMetaDto>> ObterProgressoAsync(DateTime? inicio, DateTime? fim,
        long? usuarioId, long? equipeId, string? escopo, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var ini = inicio is null ? new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc) : Utc(inicio.Value);
        var f = fim is null ? ini.AddMonths(1).AddTicks(-1) : Utc(fim.Value);
        if (f < ini) throw new RegraNegocioException("O fim do período deve ser posterior ao início.");

        var q = db.Metas.AsNoTracking()
            .Include(m => m.Usuario).Include(m => m.Equipe).Include(m => m.Funil)
            .Where(m => m.Inicio <= f && m.Fim >= ini);
        if (usuarioId is not null) q = q.Where(m => m.UsuarioId == usuarioId);
        if (equipeId is not null) q = q.Where(m => m.EquipeId == equipeId);
        switch ((escopo ?? "").ToLowerInvariant())
        {
            case "usuario": q = q.Where(m => m.UsuarioId != null); break;
            case "equipe": q = q.Where(m => m.EquipeId != null); break;
            case "empresa": q = q.Where(m => m.UsuarioId == null && m.EquipeId == null); break;
            case "": break;
            default: throw new RegraNegocioException("Escopo inválido (use usuario, equipe ou empresa).");
        }
        var metas = await q.OrderBy(m => m.Inicio).ThenBy(m => m.Nome).ToListAsync(ct);

        // Catálogo de moedas é global (EmpresaId = 0): o filtro de tenant esconderia a navegação Moeda.
        var moedaIds = metas.Where(m => m.MoedaId != null).Select(m => m.MoedaId!.Value).Distinct().ToList();
        var moedas = moedaIds.Count == 0 ? new Dictionary<long, Moeda>()
            : await db.Moedas.IgnoreQueryFilters().AsNoTracking().Where(x => moedaIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

        var membrosCache = new Dictionary<long, List<long>>();
        var linhasPorMeta = new Dictionary<long, List<Linha>>();
        foreach (var m in metas)
        {
            List<long>? responsaveis = null;
            if (m.UsuarioId is not null) responsaveis = [m.UsuarioId.Value];
            else if (m.EquipeId is not null)
            {
                if (!membrosCache.TryGetValue(m.EquipeId.Value, out responsaveis))
                {
                    responsaveis = await db.MembrosEquipe.AsNoTracking()
                        .Where(x => x.EquipeId == m.EquipeId).Select(x => x.UsuarioId).Distinct().ToListAsync(ct);
                    membrosCache[m.EquipeId.Value] = responsaveis;
                }
            }
            linhasPorMeta[m.Id] = await ObterLinhasAsync(m, responsaveis, ct);
        }

        var idsRanking = linhasPorMeta.Values.SelectMany(l => l).Where(l => l.Responsavel != null).Select(l => l.Responsavel!.Value).Distinct().ToList();
        var nomes = idsRanking.Count == 0 ? new Dictionary<long, string>()
            : await db.Usuarios.AsNoTracking().Where(u => idsRanking.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        var hoje = agora.Date;
        var resultado = new List<ProgressoMetaDto>();
        foreach (var m in metas)
        {
            var linhas = linhasPorMeta[m.Id];
            var realizado = linhas.Sum(l => l.Valor);
            var diasTotais = Math.Max(1, (m.Fim.Date - m.Inicio.Date).Days + 1);
            var decorridos = Math.Clamp((hoje - m.Inicio.Date).Days + 1, 0, diasTotais);
            decimal? projecao = decorridos == 0 ? null : Math.Round(realizado / decorridos * diasTotais, 2);
            var percentual = Math.Round(realizado / m.Alvo * 100m, 1);
            decimal? percProj = projecao is null ? null : Math.Round(projecao.Value / m.Alvo * 100m, 1);

            var porDia = linhas.GroupBy(l => l.Data.Date).ToDictionary(g => g.Key, g => g.Sum(l => l.Valor));
            var serie = new List<PontoMetaDto>();
            decimal acum = 0;
            var passos = Math.Min(diasTotais, MaxDiasSerie);
            for (var i = 0; i < passos; i++)
            {
                var dia = m.Inicio.Date.AddDays(i);
                if (porDia.TryGetValue(dia, out var v)) acum += v;
                var alvoLinear = Math.Round(m.Alvo * (i + 1) / diasTotais, 2);
                serie.Add(new PontoMetaDto(DateTime.SpecifyKind(dia, DateTimeKind.Utc), dia <= hoje ? acum : null, alvoLinear));
            }

            var ranking = m.UsuarioId is not null ? [] : linhas.Where(l => l.Responsavel != null)
                .GroupBy(l => l.Responsavel!.Value)
                .Select(g => new RankingMetaDto(g.Key, nomes.GetValueOrDefault(g.Key, "—"), g.Sum(l => l.Valor)))
                .OrderByDescending(r => r.Realizado).ThenBy(r => r.Nome).Take(5).ToList();

            resultado.Add(new ProgressoMetaDto(m.Id, m.Nome, m.Metrica.ToString(),
                m.UsuarioId is not null ? "usuario" : m.EquipeId is not null ? "equipe" : "empresa",
                m.Alvo, realizado, percentual, projecao, percProj, projecao is not null && projecao >= m.Alvo,
                m.MoedaId, m.MoedaId is null ? null : moedas.GetValueOrDefault(m.MoedaId.Value)?.Simbolo, m.MoedaId is null ? null : moedas.GetValueOrDefault(m.MoedaId.Value)?.Codigo, m.UsuarioId, m.Usuario?.Nome, m.EquipeId, m.Equipe?.Nome,
                m.FunilId, m.Funil?.Nome, m.Inicio, m.Fim, diasTotais, decorridos, serie, ranking));
        }
        return resultado;
    }

    /// <summary>Eventos (responsável, data, valor) que compõem o realizado da meta.</summary>
    private async Task<List<Linha>> ObterLinhasAsync(Meta m, List<long>? responsaveis, CancellationToken ct)
    {
        var ini = m.Inicio;
        var fim = m.Fim;
        switch (m.Metrica)
        {
            case MetricaMeta.ValorGanho:
            case MetricaMeta.QuantidadeGanhos:
            {
                var n = db.Negocios.AsNoTracking()
                    .Where(x => x.Status != null && x.Status.TipoBase == TipoStatusNegocio.Ganho
                                && x.FechadoEm != null && x.FechadoEm >= ini && x.FechadoEm <= fim);
                if (responsaveis is not null) n = n.Where(x => x.ResponsavelId != null && responsaveis.Contains(x.ResponsavelId.Value));
                if (m.FunilId is not null) n = n.Where(x => x.FunilId == m.FunilId);
                if (m.Metrica == MetricaMeta.ValorGanho)
                {
                    var moedaId = m.MoedaId;
                    var l = await n.Where(x => x.MoedaId == moedaId)
                        .Select(x => new { x.ResponsavelId, Data = x.FechadoEm!.Value, x.Valor }).ToListAsync(ct);
                    return l.Select(x => new Linha(x.ResponsavelId, x.Data, x.Valor ?? 0m)).ToList();
                }
                var l2 = await n.Select(x => new { x.ResponsavelId, Data = x.FechadoEm!.Value }).ToListAsync(ct);
                return l2.Select(x => new Linha(x.ResponsavelId, x.Data, 1m)).ToList();
            }
            case MetricaMeta.NegociosCriados:
            {
                var n = db.Negocios.AsNoTracking().Where(x => x.CriadoEm >= ini && x.CriadoEm <= fim);
                if (responsaveis is not null) n = n.Where(x => x.ResponsavelId != null && responsaveis.Contains(x.ResponsavelId.Value));
                if (m.FunilId is not null) n = n.Where(x => x.FunilId == m.FunilId);
                var l = await n.Select(x => new { x.ResponsavelId, x.CriadoEm }).ToListAsync(ct);
                return l.Select(x => new Linha(x.ResponsavelId, x.CriadoEm, 1m)).ToList();
            }
            case MetricaMeta.AtividadesConcluidas:
            {
                var a = db.Atividades.AsNoTracking()
                    .Where(x => x.Concluida && x.ConcluidaEm != null && x.ConcluidaEm >= ini && x.ConcluidaEm <= fim);
                if (responsaveis is not null) a = a.Where(x => x.ResponsavelId != null && responsaveis.Contains(x.ResponsavelId.Value));
                if (m.FunilId is not null) a = a.Where(x => x.Negocio != null && x.Negocio.FunilId == m.FunilId);
                var l = await a.Select(x => new { x.ResponsavelId, Data = x.ConcluidaEm!.Value }).ToListAsync(ct);
                return l.Select(x => new Linha(x.ResponsavelId, x.Data, 1m)).ToList();
            }
            case MetricaMeta.PropostasAprovadas:
            {
                var p = db.Propostas.AsNoTracking()
                    .Where(x => !x.IsTemplate && x.StatusAprovacao == StatusAprovacaoProposta.Aprovada);
                if (responsaveis is not null) p = p.Where(x => x.ResponsavelId != null && responsaveis.Contains(x.ResponsavelId.Value));
                if (m.FunilId is not null) p = p.Where(x => x.Negocio != null && x.Negocio.FunilId == m.FunilId);
                var props = await p.Select(x => new { x.Id, x.ResponsavelId, x.CriadoEm, x.AtualizadoEm }).ToListAsync(ct);
                if (props.Count == 0) return [];
                var ids = props.Select(x => x.Id).ToList();
                // Data da aprovação: última solicitação aprovada; sem solicitação, a última alteração da proposta.
                var aprov = await db.SolicitacoesAprovacao.AsNoTracking()
                    .Where(s => s.EntidadeAlvo == EntidadeAlvo.Proposta && s.Status == StatusSolicitacaoAprovacao.Aprovado
                                && s.AprovadoEm != null && ids.Contains(s.RegistroId))
                    .GroupBy(s => s.RegistroId).Select(g => new { Id = g.Key, Em = g.Max(s => s.AprovadoEm!.Value) })
                    .ToDictionaryAsync(g => g.Id, g => g.Em, ct);
                return props
                    .Select(x => new Linha(x.ResponsavelId, aprov.TryGetValue(x.Id, out var em) ? em : (x.AtualizadoEm ?? x.CriadoEm), 1m))
                    .Where(l => l.Data >= ini && l.Data <= fim).ToList();
            }
            default:
                return [];
        }
    }
}

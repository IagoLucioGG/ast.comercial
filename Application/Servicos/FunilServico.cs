using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class FunilServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IFunilServico
{
    // Retorna a entidade (e não DTO) para casar com o EDM "Funis" e permitir $expand/$select/$count.
    public IQueryable<Funil> ObterTodos()
        => db.Funis.AsNoTracking();

    public async Task<Funil?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Funis.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Funil> CriarAsync(Delta<Funil> delta, CancellationToken ct = default)
    {
        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var funil = new Funil { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(funil);
        funil.EmpresaId = db.EmpresaIdAtual;
        ValidarNomeFunil(funil);
        if (!alterados.Contains(nameof(Funil.Ordem)))
            funil.Ordem = ((await db.Funis.MaxAsync(f => (int?)f.Ordem, ct)) ?? -1) + 1;

        db.Funis.Add(funil);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return funil;
    }

    public async Task<Funil?> AtualizarAsync(long id, Delta<Funil> delta, CancellationToken ct = default)
    {
        var funil = await db.Funis.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (funil is null) return null;

        var empresaId = funil.EmpresaId;
        delta.Patch(funil);
        funil.EmpresaId = empresaId;
        ValidarNomeFunil(funil);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return funil;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var funil = await db.Funis.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (funil is null) return false;

        var abertos = await db.Negocios.CountAsync(n => n.FunilId == id && n.FechadoEm == null, ct);
        if (abertos > 0)
            throw new RegraNegocioException(
                $"Não é possível arquivar o funil \"{funil.Nome}\": há {abertos} negócio(s) aberto(s). Mova, ganhe ou perca esses negócios antes.");

        funil.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public IQueryable<EtapaFunil> ObterEtapas(long funilId)
        => db.EtapasFunil.AsNoTracking().Where(e => e.FunilId == funilId);

    public async Task<EtapaFunil> CriarEtapaAsync(long funilId, Delta<EtapaFunil> delta, CancellationToken ct = default)
    {
        var funilExiste = await db.Funis.AnyAsync(f => f.Id == funilId, ct);
        if (!funilExiste) throw new KeyNotFoundException($"Funil {funilId} não encontrado.");

        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var etapa = new EtapaFunil();
        delta.Patch(etapa);
        etapa.FunilId = funilId;
        etapa.EmpresaId = db.EmpresaIdAtual;
        ValidarEtapa(etapa);
        if (!alterados.Contains(nameof(EtapaFunil.Ordem)))
            etapa.Ordem = ((await db.EtapasFunil.Where(e => e.FunilId == funilId).MaxAsync(e => (int?)e.Ordem, ct)) ?? -1) + 1;

        db.EtapasFunil.Add(etapa);
        await db.SaveChangesAsync(ct);

        return etapa;
    }

    public async Task<EtapaFunil?> AtualizarEtapaAsync(long funilId, long etapaId, Delta<EtapaFunil> delta, CancellationToken ct = default)
    {
        var etapa = await db.EtapasFunil.FirstOrDefaultAsync(e => e.Id == etapaId && e.FunilId == funilId, ct);
        if (etapa is null) return null;

        var empresaId = etapa.EmpresaId;
        delta.Patch(etapa);
        etapa.FunilId = funilId; // a etapa não troca de funil
        etapa.EmpresaId = empresaId;
        ValidarEtapa(etapa);

        await db.SaveChangesAsync(ct);
        return etapa;
    }

    public async Task<bool> RemoverEtapaAsync(long funilId, long etapaId, CancellationToken ct = default)
    {
        var etapa = await db.EtapasFunil.FirstOrDefaultAsync(e => e.Id == etapaId && e.FunilId == funilId, ct);
        if (etapa is null) return false;

        var abertos = await db.Negocios.CountAsync(n => n.EtapaId == etapaId && n.FechadoEm == null, ct);
        if (abertos > 0)
            throw new RegraNegocioException(
                $"Não é possível remover a etapa \"{etapa.Nome}\": há {abertos} negócio(s) aberto(s) nela. Mova esses negócios para outra etapa antes.");

        etapa.Ativo = false; // inativa (os negócios fechados mantêm o vínculo)
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ReordenarEtapasAsync(long funilId, IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        if (!await db.Funis.AnyAsync(f => f.Id == funilId, ct))
            throw new KeyNotFoundException($"Funil {funilId} não encontrado.");
        if (ids.Distinct().Count() != ids.Count)
            throw new RegraNegocioException("A lista de etapas para ordenar tem repetidas.");

        var etapas = await db.EtapasFunil.Where(e => e.FunilId == funilId).ToListAsync(ct);
        var porId = etapas.ToDictionary(e => e.Id);
        if (ids.Any(i => !porId.ContainsKey(i)))
            throw new RegraNegocioException("Há etapas que não pertencem a este funil.");

        var ordem = 0;
        foreach (var id in ids) porId[id].Ordem = ordem++;
        foreach (var restante in etapas.Where(e => !ids.Contains(e.Id)).OrderBy(e => e.Ordem).ThenBy(e => e.Id))
            restante.Ordem = ordem++;

        await db.SaveChangesAsync(ct);
    }

    private static void ValidarNomeFunil(Funil funil)
    {
        funil.Nome = (funil.Nome ?? string.Empty).Trim();
        if (funil.Nome.Length == 0) throw new RegraNegocioException("Informe o nome do funil.");
        if (funil.Nome.Length > 200) throw new RegraNegocioException("O nome do funil pode ter até 200 caracteres.");
    }

    private static void ValidarEtapa(EtapaFunil etapa)
    {
        etapa.Nome = (etapa.Nome ?? string.Empty).Trim();
        if (etapa.Nome.Length == 0) throw new RegraNegocioException("Informe o nome da etapa.");
        if (etapa.Nome.Length > 200) throw new RegraNegocioException("O nome da etapa pode ter até 200 caracteres.");
        if (etapa.DiasParaExpirar is < 0) throw new RegraNegocioException("Os dias para expirar não podem ser negativos.");
    }
}

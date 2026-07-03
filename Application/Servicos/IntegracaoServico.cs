using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class IntegracaoServico(AppDbContext db) : IIntegracaoServico
{
    public IQueryable<Integracao> ObterTodos() => db.Integracoes.AsNoTracking();

    public async Task<Integracao?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Integracoes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Integracao> CriarAsync(Delta<Integracao> delta, CancellationToken ct = default)
    {
        var entidade = new Integracao
        {
            EmpresaId = db.EmpresaIdAtual,
            ChaveWebhook = Guid.NewGuid().ToString("N")
        };
        delta.Patch(entidade);
        db.Integracoes.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Integracao?> AtualizarAsync(long id, Delta<Integracao> delta, CancellationToken ct = default)
    {
        var entidade = await db.Integracoes.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Integracoes.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Integracao?> ObterPorChaveWebhookAsync(string chave, CancellationToken ct = default)
        => await db.Integracoes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ChaveWebhook == chave && i.Ativo && i.Status == StatusIntegracao.Ligado, ct);

    // --- Mapeamentos ---

    public IQueryable<MapeamentoCampoIntegracao> ObterMapeamentos() => db.MapeamentosIntegracao.AsNoTracking();

    public async Task<MapeamentoCampoIntegracao> CriarMapeamentoAsync(Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = new MapeamentoCampoIntegracao { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.MapeamentosIntegracao.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<MapeamentoCampoIntegracao?> AtualizarMapeamentoAsync(long id, Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = await db.MapeamentosIntegracao.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverMapeamentoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.MapeamentosIntegracao.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // --- Fluxos ---

    public IQueryable<FluxoIntegracao> ObterFluxos() => db.FluxosIntegracao.AsNoTracking();

    public async Task<FluxoIntegracao> CriarFluxoAsync(Delta<FluxoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = new FluxoIntegracao { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.FluxosIntegracao.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<FluxoIntegracao?> AtualizarFluxoAsync(long id, Delta<FluxoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = await db.FluxosIntegracao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverFluxoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.FluxosIntegracao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // --- Etapas ---

    public IQueryable<EtapaFluxoIntegracao> ObterEtapas() => db.EtapasFluxoIntegracao.AsNoTracking();

    public async Task<EtapaFluxoIntegracao> CriarEtapaAsync(Delta<EtapaFluxoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = new EtapaFluxoIntegracao { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.EtapasFluxoIntegracao.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<EtapaFluxoIntegracao?> AtualizarEtapaAsync(long id, Delta<EtapaFluxoIntegracao> delta, CancellationToken ct = default)
    {
        var entidade = await db.EtapasFluxoIntegracao.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverEtapaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.EtapasFluxoIntegracao.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // --- Logs ---

    public IQueryable<LogIntegracao> ObterLogs() => db.LogsIntegracao.AsNoTracking();
}

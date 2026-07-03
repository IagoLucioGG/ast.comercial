using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class CampoServico(AppDbContext db) : ICampoServico
{
    public IQueryable<Campo> ObterTodos()
        => db.Campos.AsNoTracking();

    public async Task<Campo?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Campos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Campo> CriarAsync(Delta<Campo> delta, CancellationToken ct = default)
    {
        var entidade = new Campo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        entidade.GerarChave();
        db.Campos.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Campo?> AtualizarAsync(long id, Delta<Campo> delta, CancellationToken ct = default)
    {
        var entidade = await db.Campos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Campos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<OpcaoCampo> ObterOpcoes()
        => db.OpcoesCampo.AsNoTracking();

    public async Task<OpcaoCampo> CriarOpcaoAsync(Delta<OpcaoCampo> delta, CancellationToken ct = default)
    {
        var entidade = new OpcaoCampo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.OpcoesCampo.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<OpcaoCampo?> AtualizarOpcaoAsync(long id, Delta<OpcaoCampo> delta, CancellationToken ct = default)
    {
        var entidade = await db.OpcoesCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverOpcaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.OpcoesCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<RegraCampo> ObterRegras()
        => db.RegrasCampo.AsNoTracking();

    public async Task<RegraCampo> CriarRegraAsync(Delta<RegraCampo> delta, CancellationToken ct = default)
    {
        var entidade = new RegraCampo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.RegrasCampo.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<RegraCampo?> AtualizarRegraAsync(long id, Delta<RegraCampo> delta, CancellationToken ct = default)
    {
        var entidade = await db.RegrasCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverRegraAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.RegrasCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<ConfiguracaoCampo> ObterConfiguracoes()
        => db.ConfiguracoesCampo.AsNoTracking();

    public async Task<ConfiguracaoCampo> CriarConfiguracaoAsync(Delta<ConfiguracaoCampo> delta, CancellationToken ct = default)
    {
        var entidade = new ConfiguracaoCampo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.ConfiguracoesCampo.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ConfiguracaoCampo?> AtualizarConfiguracaoAsync(long id, Delta<ConfiguracaoCampo> delta, CancellationToken ct = default)
    {
        var entidade = await db.ConfiguracoesCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverConfiguracaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ConfiguracoesCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<ValorCampo> ObterValores()
        => db.ValoresCampo.AsNoTracking();

    public async Task<ValorCampo> CriarValorAsync(Delta<ValorCampo> delta, CancellationToken ct = default)
    {
        var entidade = new ValorCampo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.ValoresCampo.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ValorCampo?> AtualizarValorAsync(long id, Delta<ValorCampo> delta, CancellationToken ct = default)
    {
        var entidade = await db.ValoresCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverValorAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ValoresCampo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

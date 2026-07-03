using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class FormularioServico(AppDbContext db) : IFormularioServico
{
    public IQueryable<Formulario> ObterTodos() => db.Formularios.AsNoTracking();

    public async Task<Formulario?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Formularios.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Formulario> CriarAsync(Delta<Formulario> delta, CancellationToken ct = default)
    {
        var entidade = new Formulario { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.Formularios.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Formulario?> AtualizarAsync(long id, Delta<Formulario> delta, CancellationToken ct = default)
    {
        var entidade = await db.Formularios.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Formularios.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<SecaoFormulario> ObterSecoes() => db.SecoesFormulario.AsNoTracking();

    public async Task<SecaoFormulario> CriarSecaoAsync(Delta<SecaoFormulario> delta, CancellationToken ct = default)
    {
        var entidade = new SecaoFormulario { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.SecoesFormulario.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<SecaoFormulario?> AtualizarSecaoAsync(long id, Delta<SecaoFormulario> delta, CancellationToken ct = default)
    {
        var entidade = await db.SecoesFormulario.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.SecoesFormulario.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<CampoFormulario> ObterCampos() => db.CamposFormulario.AsNoTracking();

    public async Task<CampoFormulario> CriarCampoAsync(Delta<CampoFormulario> delta, CancellationToken ct = default)
    {
        var entidade = new CampoFormulario { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.CamposFormulario.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<CampoFormulario?> AtualizarCampoAsync(long id, Delta<CampoFormulario> delta, CancellationToken ct = default)
    {
        var entidade = await db.CamposFormulario.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverCampoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.CamposFormulario.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

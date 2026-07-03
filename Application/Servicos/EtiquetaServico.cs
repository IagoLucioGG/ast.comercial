using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class EtiquetaServico(AppDbContext db) : IEtiquetaServico
{
    public IQueryable<Etiqueta> ObterTodos()
        => db.Etiquetas.AsNoTracking();

    public async Task<Etiqueta?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Etiquetas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Etiqueta> CriarAsync(Delta<Etiqueta> delta, CancellationToken ct = default)
    {
        var entidade = new Etiqueta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.Etiquetas.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Etiqueta?> AtualizarAsync(long id, Delta<Etiqueta> delta, CancellationToken ct = default)
    {
        var entidade = await db.Etiquetas.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Etiquetas.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

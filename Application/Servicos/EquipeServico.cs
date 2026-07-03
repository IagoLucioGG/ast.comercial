using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class EquipeServico(AppDbContext db) : IEquipeServico
{
    public IQueryable<Equipe> ObterTodos()
        => db.Equipes.AsNoTracking();

    public async Task<Equipe?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Equipes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public IQueryable<MembroEquipe> ObterMembros()
        => db.MembrosEquipe.AsNoTracking();

    public async Task<MembroEquipe> CriarMembroAsync(Delta<MembroEquipe> delta, CancellationToken ct = default)
    {
        var entidade = new MembroEquipe { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.MembrosEquipe.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverMembroAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.MembrosEquipe.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

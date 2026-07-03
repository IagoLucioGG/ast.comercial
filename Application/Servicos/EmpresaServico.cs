using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class EmpresaServico(AppDbContext db) : IEmpresaServico
{
    public IQueryable<Empresa> ObterTodos()
        => db.Empresas.AsNoTracking();

    public async Task<Empresa?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
}

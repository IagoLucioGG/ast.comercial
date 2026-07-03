using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class DepartamentoServico(AppDbContext db) : IDepartamentoServico
{
    public IQueryable<Departamento> ObterTodos()
        => db.Departamentos.AsNoTracking();

    public async Task<Departamento?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
}

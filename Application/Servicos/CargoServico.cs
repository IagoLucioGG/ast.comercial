using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class CargoServico(AppDbContext db) : ICargoServico
{
    public IQueryable<Cargo> ObterTodos()
        => db.Cargos.AsNoTracking();

    public async Task<Cargo?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Cargos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
}

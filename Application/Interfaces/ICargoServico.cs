using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface ICargoServico
{
    IQueryable<Cargo> ObterTodos();
    Task<Cargo?> ObterPorIdAsync(long id, CancellationToken ct = default);
}

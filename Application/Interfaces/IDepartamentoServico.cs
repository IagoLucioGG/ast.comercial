using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface IDepartamentoServico
{
    IQueryable<Departamento> ObterTodos();
    Task<Departamento?> ObterPorIdAsync(long id, CancellationToken ct = default);
}

using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface IEmpresaServico
{
    IQueryable<Empresa> ObterTodos();
    Task<Empresa?> ObterPorIdAsync(long id, CancellationToken ct = default);
}

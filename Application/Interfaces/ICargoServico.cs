using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface ICargoServico
{
    IQueryable<Cargo> ObterTodos();
    Task<Cargo?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Cargo> CriarAsync(Delta<Cargo> delta, CancellationToken ct = default);
    Task<Cargo?> AtualizarAsync(long id, Delta<Cargo> delta, CancellationToken ct = default);
    /// <summary>Exclusão lógica (Ativo=false).</summary>
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
}

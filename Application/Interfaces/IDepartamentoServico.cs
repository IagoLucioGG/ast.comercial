using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IDepartamentoServico
{
    IQueryable<Departamento> ObterTodos();
    Task<Departamento?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Departamento> CriarAsync(Delta<Departamento> delta, CancellationToken ct = default);
    Task<Departamento?> AtualizarAsync(long id, Delta<Departamento> delta, CancellationToken ct = default);
    /// <summary>Exclusão lógica (Ativo=false).</summary>
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
}

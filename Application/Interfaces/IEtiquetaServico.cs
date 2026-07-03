using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IEtiquetaServico
{
    IQueryable<Etiqueta> ObterTodos();
    Task<Etiqueta?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Etiqueta> CriarAsync(Delta<Etiqueta> delta, CancellationToken ct = default);
    Task<Etiqueta?> AtualizarAsync(long id, Delta<Etiqueta> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
}

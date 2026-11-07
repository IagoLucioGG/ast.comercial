using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IMetaServico
{
    IQueryable<Meta> ObterTodos();
    Task<Meta?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Meta> CriarAsync(Delta<Meta> delta, CancellationToken ct = default);
    Task<Meta?> AtualizarAsync(long id, Delta<Meta> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ProgressoMetaDto>> ObterProgressoAsync(DateTime? inicio, DateTime? fim, long? usuarioId, long? equipeId, string? escopo, CancellationToken ct = default);
}

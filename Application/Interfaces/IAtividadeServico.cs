using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IAtividadeServico
{
    IQueryable<Atividade> ObterTodos();
    Task<Atividade?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Atividade> CriarAsync(Delta<Atividade> delta, CancellationToken cancellationToken = default);
    Task<Atividade?> AtualizarAsync(long id, Delta<Atividade> delta, CancellationToken cancellationToken = default);
    Task<bool> ConcluirAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default);
    IQueryable<object> ObterTipos();
}

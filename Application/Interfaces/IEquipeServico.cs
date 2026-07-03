using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IEquipeServico
{
    IQueryable<Equipe> ObterTodos();
    Task<Equipe?> ObterPorIdAsync(long id, CancellationToken ct = default);

    IQueryable<MembroEquipe> ObterMembros();
    Task<MembroEquipe> CriarMembroAsync(Delta<MembroEquipe> delta, CancellationToken ct = default);
    Task<bool> RemoverMembroAsync(long id, CancellationToken ct = default);
}

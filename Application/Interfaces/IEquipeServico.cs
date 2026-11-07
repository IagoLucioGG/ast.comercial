using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IEquipeServico
{
    IQueryable<Equipe> ObterTodos();
    Task<Equipe?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Equipe> CriarAsync(Delta<Equipe> delta, CancellationToken ct = default);
    Task<Equipe?> AtualizarAsync(long id, Delta<Equipe> delta, CancellationToken ct = default);
    /// <summary>Exclusão lógica da equipe e dos vínculos de membros.</summary>
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    IQueryable<MembroEquipe> ObterMembros();
    Task<MembroEquipe> CriarMembroAsync(long equipeId, long usuarioId, PapelEquipe papel, CancellationToken ct = default);
    Task<MembroEquipe?> AtualizarMembroAsync(long id, PapelEquipe papel, CancellationToken ct = default);
    Task<bool> RemoverMembroAsync(long id, CancellationToken ct = default);
}

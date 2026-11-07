using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IUsuarioServico
{
    IQueryable<Usuario> ObterTodos();
    /// <summary>Todos os usuários da empresa atual, inclusive inativos.</summary>
    IQueryable<Usuario> ObterTodosInclusiveInativos();
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Usuario> CriarAsync(NovoUsuarioRequest request, CancellationToken ct = default);
    Task<Usuario?> AtualizarAsync(long id, Delta<Usuario> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task<string> RegenerarTokenIntegracaoAsync(long id, CancellationToken ct = default);
    Task AlterarSenhaAsync(long id, string senhaAtual, string novaSenha, CancellationToken ct = default);
    /// <summary>Redefine a senha sem exigir a atual (uso administrativo) e revoga os tokens de acesso do usuário.</summary>
    Task RedefinirSenhaAsync(long id, string novaSenha, CancellationToken ct = default);
}

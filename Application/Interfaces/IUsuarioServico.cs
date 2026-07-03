using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IUsuarioServico
{
    IQueryable<Usuario> ObterTodos();
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Usuario> CriarAsync(Delta<Usuario> delta, CancellationToken ct = default);
    Task<Usuario?> AtualizarAsync(long id, Delta<Usuario> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task<string> RegenerarTokenIntegracaoAsync(long id, CancellationToken ct = default);
    Task AlterarSenhaAsync(long id, string senhaAtual, string novaSenha, CancellationToken ct = default);
}

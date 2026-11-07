using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IPessoaContatoServico
{
    IQueryable<PessoaContato> ObterTodos();
    Task<PessoaContato?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<PessoaContato> CriarAsync(Delta<PessoaContato> delta, CancellationToken ct = default);
    Task<PessoaContato?> AtualizarAsync(long id, Delta<PessoaContato> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
}

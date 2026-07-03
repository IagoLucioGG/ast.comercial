using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IPropostaServico
{
    IQueryable<Proposta> ObterTodos();
    Task<Proposta?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Proposta> CriarAsync(Delta<Proposta> delta, CancellationToken ct = default);
    Task<Proposta?> AtualizarAsync(long id, Delta<Proposta> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task AprovarAsync(long id, CancellationToken ct = default);
    Task RejeitarAsync(long id, CancellationToken ct = default);

    IQueryable<ItemProposta> ObterProdutos();
    Task<ItemProposta> CriarProdutoAsync(Delta<ItemProposta> delta, CancellationToken ct = default);
    Task<ItemProposta?> AtualizarProdutoAsync(long id, Delta<ItemProposta> delta, CancellationToken ct = default);
    Task<bool> RemoverProdutoAsync(long id, CancellationToken ct = default);

    IQueryable<SecaoProposta> ObterSecoes();
    Task<SecaoProposta> CriarSecaoAsync(Delta<SecaoProposta> delta, CancellationToken ct = default);
    Task<SecaoProposta?> AtualizarSecaoAsync(long id, Delta<SecaoProposta> delta, CancellationToken ct = default);
    Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default);

    IQueryable<ParcelaProposta> ObterParcelas();
    Task<ParcelaProposta> CriarParcelaAsync(Delta<ParcelaProposta> delta, CancellationToken ct = default);
    Task<ParcelaProposta?> AtualizarParcelaAsync(long id, Delta<ParcelaProposta> delta, CancellationToken ct = default);
    Task<bool> RemoverParcelaAsync(long id, CancellationToken ct = default);
}

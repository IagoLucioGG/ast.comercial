using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IProdutoServico
{
    IQueryable<Produto> ObterTodos();
    Task<Produto?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Produto> CriarAsync(Delta<Produto> delta, CancellationToken ct = default);
    Task<Produto?> AtualizarAsync(long id, Delta<Produto> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    IQueryable<FamiliaProduto> ObterFamilias();
    Task<FamiliaProduto> CriarFamiliaAsync(Delta<FamiliaProduto> delta, CancellationToken ct = default);
    Task<FamiliaProduto?> AtualizarFamiliaAsync(long id, Delta<FamiliaProduto> delta, CancellationToken ct = default);
    Task<bool> RemoverFamiliaAsync(long id, CancellationToken ct = default);

    IQueryable<GrupoProduto> ObterGrupos();
    Task<GrupoProduto> CriarGrupoAsync(Delta<GrupoProduto> delta, CancellationToken ct = default);
    Task<GrupoProduto?> AtualizarGrupoAsync(long id, Delta<GrupoProduto> delta, CancellationToken ct = default);
    Task<bool> RemoverGrupoAsync(long id, CancellationToken ct = default);
}

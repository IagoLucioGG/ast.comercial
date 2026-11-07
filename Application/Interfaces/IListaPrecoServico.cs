using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IListaPrecoServico
{
    IQueryable<ListaPreco> ObterTodas();
    Task<ListaPreco?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<ListaPreco> CriarAsync(Delta<ListaPreco> delta, CancellationToken ct = default);
    Task<ListaPreco?> AtualizarAsync(long id, Delta<ListaPreco> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    IQueryable<ItemListaPreco> ObterItens();
    Task<ItemListaPreco?> ObterItemPorIdAsync(long id, CancellationToken ct = default);
    Task<ItemListaPreco> CriarItemAsync(Delta<ItemListaPreco> delta, CancellationToken ct = default);
    Task<ItemListaPreco?> AtualizarItemAsync(long id, Delta<ItemListaPreco> delta, CancellationToken ct = default);
    Task<bool> RemoverItemAsync(long id, CancellationToken ct = default);

    /// <summary>Copia o preço base dos produtos para a lista. Devolve (criados, atualizados, ignorados).</summary>
    Task<ResultadoCopiaPrecos> CopiarPrecosBaseAsync(long listaId, bool sobrescrever, CancellationToken ct = default);

    /// <summary>Preço base do produto e preço em cada lista (somente as vigentes, salvo <paramref name="todas"/>).</summary>
    Task<PrecosProdutoDto?> ObterPrecosProdutoAsync(long produtoId, bool todas, CancellationToken ct = default);
}

public record ResultadoCopiaPrecos(int Criados, int Atualizados, int Ignorados);

public record PrecoListaDto(
    long ListaPrecoId, string ListaNome, bool Padrao, bool Vigente,
    DateTime? VigenciaInicio, DateTime? VigenciaFim,
    long? MoedaId, string? MoedaCodigo, string? MoedaSimbolo,
    long? ItemId, decimal? Preco);

public record PrecosProdutoDto(
    long ProdutoId, string Nome, decimal Preco,
    long? MoedaId, string? MoedaCodigo, string? MoedaSimbolo,
    IReadOnlyList<PrecoListaDto> Listas);

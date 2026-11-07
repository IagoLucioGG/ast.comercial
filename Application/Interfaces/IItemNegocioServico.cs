using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

/// <summary>Preço sugerido de um produto: da lista de preço padrão vigente ou, na falta, do cadastro do produto.</summary>
public record PrecoSugeridoDto(decimal Preco, string Origem, long? ListaPrecoId, string? ListaPrecoNome);

/// <summary>Resultado de "atualizar valor do negócio pela soma dos itens".</summary>
public record ValorPelosItensDto(long NegocioId, decimal Valor, long? MoedaId, int QuantidadeItens);

public interface IItemNegocioServico
{
    IQueryable<ItemNegocio> ObterTodos();
    Task<ItemNegocio> CriarAsync(Delta<ItemNegocio> delta, CancellationToken ct = default);
    Task<ItemNegocio?> AtualizarAsync(long id, Delta<ItemNegocio> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task ReordenarAsync(long negocioId, IReadOnlyList<long> ids, CancellationToken ct = default);
    Task<ValorPelosItensDto> AtualizarValorPelosItensAsync(long negocioId, CancellationToken ct = default);
    Task<PrecoSugeridoDto> ObterPrecoSugeridoAsync(long produtoId, long? negocioId, CancellationToken ct = default);
}

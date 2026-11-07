using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IPedidoServico
{
    IQueryable<Pedido> ObterTodos();
    Task<Pedido?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Pedido> CriarAsync(Delta<Pedido> delta, CancellationToken ct = default);
    Task<Pedido?> AtualizarAsync(long id, Delta<Pedido> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    /// <summary>Gera o pedido a partir de uma proposta aprovada/aceita, copiando itens e valores. Null se a proposta não existe.</summary>
    Task<Pedido?> GerarDePropostaAsync(long propostaId, CancellationToken ct = default);

    IQueryable<EtapaPedido> ObterEtapas();
    Task<EtapaPedido> CriarEtapaAsync(Delta<EtapaPedido> delta, CancellationToken ct = default);
    Task<EtapaPedido?> AtualizarEtapaAsync(long id, Delta<EtapaPedido> delta, CancellationToken ct = default);
    Task<bool> RemoverEtapaAsync(long id, CancellationToken ct = default);
}

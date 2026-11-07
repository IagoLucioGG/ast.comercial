using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class PedidosController(IPedidoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Pedido> Get() => servico.ObterTodos();

    /// <summary>SingleResult permite $expand=Itens,Cliente,Etapa também na leitura de um registro.</summary>
    [EnableQuery]
    public SingleResult<Pedido> Get(long key) => SingleResult.Create(servico.ObterTodos().Where(p => p.Id == key));

    public async Task<ActionResult> Post([FromBody] Delta<Pedido> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Pedido> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    /// <summary>
    /// Gera o pedido de uma proposta aprovada ou aceita. Corpo: { "propostaId": 1 }.
    /// A rota fica em api/pedidos para a permissão ser "Criar pedido" (e não "Editar proposta").
    /// </summary>
    [HttpPost("api/pedidos/de-proposta")]
    public async Task<ActionResult> GerarDeProposta([FromBody] GerarPedidoRequest corpo, CancellationToken ct)
    {
        var pedido = await servico.GerarDePropostaAsync(corpo.PropostaId, ct);
        return pedido is null ? NotFound() : Ok(new { pedido.Id, pedido.Numero });
    }
}

public record GerarPedidoRequest(long PropostaId);

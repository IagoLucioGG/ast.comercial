using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

/// <summary>Etapas do processo de venda (Orders@Stages da Ploomes).</summary>
public class EtapasPedidoController(IPedidoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<EtapaPedido> Get() => servico.ObterEtapas();

    public async Task<ActionResult> Post([FromBody] Delta<EtapaPedido> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarEtapaAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<EtapaPedido> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarEtapaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverEtapaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

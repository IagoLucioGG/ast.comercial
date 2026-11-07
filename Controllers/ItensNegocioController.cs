using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

/// <summary>Corpo de ReordenarItens: ids dos itens na nova ordem.</summary>
public record ReordenarItensRequest(List<long>? Ids);

/// <summary>
/// Produtos do negócio. CRUD em odata/ItensNegocio (o Total é sempre calculado no servidor) e operações compostas:
/// POST odata/Negocios({id})/ReordenarItens { "Ids": [..] } · POST odata/Negocios({id})/AtualizarValorPelosItens ·
/// GET odata/ItensNegocio@PrecoSugerido?produtoId=..&amp;negocioId=..
/// </summary>
public class ItensNegocioController(IItemNegocioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ItemNegocio> Get() => servico.ObterTodos();

    [EnableQuery]
    public SingleResult<ItemNegocio> Get(long key) => SingleResult.Create(servico.ObterTodos().Where(i => i.Id == key));

    public async Task<ActionResult> Post([FromBody] Delta<ItemNegocio> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ItemNegocio> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpPost("odata/Negocios({key})/ReordenarItens")]
    public async Task<ActionResult> Reordenar(long key, [FromBody] ReordenarItensRequest? corpo, CancellationToken ct)
    {
        if (corpo?.Ids is null) return BadRequest(new { erro = "Informe a lista de ids na nova ordem.", status = 400 });
        await servico.ReordenarAsync(key, corpo.Ids, ct);
        return NoContent();
    }

    [HttpPost("odata/Negocios({key})/AtualizarValorPelosItens")]
    public async Task<ActionResult> AtualizarValorPelosItens(long key, CancellationToken ct)
    {
        var resultado = await servico.AtualizarValorPelosItensAsync(key, ct);
        return Ok(resultado);
    }

    [HttpGet("odata/ItensNegocio@PrecoSugerido")]
    public async Task<ActionResult> PrecoSugerido([FromQuery] long produtoId, [FromQuery] long? negocioId, CancellationToken ct)
    {
        var resultado = await servico.ObterPrecoSugeridoAsync(produtoId, negocioId, ct);
        return Ok(resultado);
    }
}

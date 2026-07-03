using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class EtiquetasController(IEtiquetaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Etiqueta> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Etiqueta>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Etiqueta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Etiqueta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

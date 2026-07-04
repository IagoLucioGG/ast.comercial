using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class SecoesFormularioController(IFormularioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<SecaoFormulario> Get() => servico.ObterSecoes();

    public async Task<ActionResult> Post([FromBody] Delta<SecaoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarSecaoAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<SecaoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarSecaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverSecaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

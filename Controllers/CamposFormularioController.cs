using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class CamposFormularioController(IFormularioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<CampoFormulario> Get() => servico.ObterCampos();

    public async Task<ActionResult> Post([FromBody] Delta<CampoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarCampoAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<CampoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarCampoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverCampoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

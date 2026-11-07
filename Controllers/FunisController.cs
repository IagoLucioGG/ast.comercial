using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class FunisController(IFunilServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Funil> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Funil>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult<Funil>> Post([FromBody] Delta<Funil> delta)
    {
        var resultado = await servico.CriarAsync(delta);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Funil> delta)
    {
        var resultado = await servico.AtualizarAsync(key, delta);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key)
    {
        var removido = await servico.RemoverAsync(key);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Funis({key})/Etapas")]
    public IQueryable<EtapaFunil> ObterEtapas(long key) => servico.ObterEtapas(key);

    [HttpPost("odata/Funis({key})/Etapas")]
    public async Task<ActionResult<EtapaFunil>> CriarEtapa(long key, [FromBody] Delta<EtapaFunil> delta)
    {
        var resultado = await servico.CriarEtapaAsync(key, delta);
        return Created(resultado);
    }
}

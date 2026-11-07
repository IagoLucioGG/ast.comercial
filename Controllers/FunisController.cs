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

    /// <summary>Edita nome, ordem e dias para expirar da etapa.</summary>
    [HttpPatch("odata/Funis({key})/Etapas({etapaId})")]
    public async Task<ActionResult> AtualizarEtapa(long key, long etapaId, [FromBody] Delta<EtapaFunil> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarEtapaAsync(key, etapaId, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>Inativa a etapa. 422 se houver negócios abertos nela.</summary>
    [HttpDelete("odata/Funis({key})/Etapas({etapaId})")]
    public async Task<ActionResult> RemoverEtapa(long key, long etapaId, CancellationToken ct)
    {
        var removido = await servico.RemoverEtapaAsync(key, etapaId, ct);
        return removido ? NoContent() : NotFound();
    }

    /// <summary>Corpo: { "Ids": [etapa1, etapa2, ...] } na nova ordem.</summary>
    [HttpPost("odata/Funis({key})/ReordenarEtapas")]
    public async Task<ActionResult> ReordenarEtapas(long key, [FromBody] ReordenarItensRequest? corpo, CancellationToken ct)
    {
        if (corpo?.Ids is null) return BadRequest(new { erro = "Informe a lista de ids na nova ordem.", status = 400 });
        await servico.ReordenarEtapasAsync(key, corpo.Ids, ct);
        return NoContent();
    }
}

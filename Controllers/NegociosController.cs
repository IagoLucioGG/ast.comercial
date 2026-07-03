using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class NegociosController(INegocioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Negocio> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Negocio>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Negocio> delta)
    {
        var resultado = await servico.CriarAsync(delta);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Negocio> delta)
    {
        var resultado = await servico.AtualizarAsync(key, delta);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key)
    {
        var removido = await servico.RemoverAsync(key);
        return removido ? NoContent() : NotFound();
    }

    [HttpPost("odata/Negocios({key})/Ganhar")]
    public async Task<ActionResult> Ganhar(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.GanharAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("odata/Negocios({key})/Perder")]
    public async Task<ActionResult> Perder(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.PerderAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("odata/Negocios({key})/Reabrir")]
    public async Task<ActionResult> Reabrir(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.ReabrirAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [EnableQuery]
    [HttpGet("odata/Negocios@Status")]
    public IQueryable<StatusNegocio> ObterStatus() => servico.ObterStatus();

    [HttpPost("odata/Negocios@Status")]
    public async Task<ActionResult> CriarStatus([FromBody] Delta<StatusNegocio> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarStatusAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Negocios@Status({key})")]
    public async Task<ActionResult> AtualizarStatus(long key, [FromBody] Delta<StatusNegocio> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarStatusAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Negocios@Status({key})")]
    public async Task<ActionResult> RemoverStatus(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverStatusAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Negocios@MotivosPerda")]
    public IQueryable<MotivoPerda> ObterMotivosPerda() => servico.ObterMotivosPerda();

    [HttpPost("odata/Negocios@MotivosPerda")]
    public async Task<ActionResult> CriarMotivoPerda([FromBody] Delta<MotivoPerda> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarMotivoPerdaAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Negocios@MotivosPerda({key})")]
    public async Task<ActionResult> AtualizarMotivoPerda(long key, [FromBody] Delta<MotivoPerda> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarMotivoPerdaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Negocios@MotivosPerda({key})")]
    public async Task<ActionResult> RemoverMotivoPerda(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverMotivoPerdaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Negocios@Funis")]
    public IQueryable<Funil> ObterFunis() => servico.ObterFunis();

    [EnableQuery]
    [HttpGet("odata/Negocios@Etapas")]
    public IQueryable<EtapaFunil> ObterEtapas() => servico.ObterEtapas();
}

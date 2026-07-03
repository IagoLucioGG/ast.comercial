using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class ClientesController(IClienteServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Cliente> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Cliente>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Cliente> delta)
    {
        var resultado = await servico.CriarAsync(delta);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Cliente> delta)
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
    [HttpGet("odata/Clientes@Origens")]
    public IQueryable<OrigemContato> ObterOrigens() => servico.ObterOrigens();

    [HttpPost("odata/Clientes@Origens")]
    public async Task<ActionResult> CriarOrigem([FromBody] Delta<OrigemContato> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarOrigemAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Clientes@Origens({key})")]
    public async Task<ActionResult> AtualizarOrigem(long key, [FromBody] Delta<OrigemContato> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarOrigemAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Clientes@Origens({key})")]
    public async Task<ActionResult> RemoverOrigem(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverOrigemAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Clientes@StatusClientes")]
    public IQueryable<StatusCliente> ObterStatusClientes() => servico.ObterStatusClientes();

    [HttpPost("odata/Clientes@Status")]
    public async Task<ActionResult> CriarStatus([FromBody] Delta<StatusCliente> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarStatusAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Clientes@Status({key})")]
    public async Task<ActionResult> AtualizarStatus(long key, [FromBody] Delta<StatusCliente> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarStatusAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Clientes@Status({key})")]
    public async Task<ActionResult> RemoverStatus(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverStatusAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Clientes@RamosAtividade")]
    public IQueryable<RamoAtividade> ObterRamos() => servico.ObterRamos();

    [HttpPost("odata/Clientes@RamosAtividade")]
    public async Task<ActionResult> CriarRamo([FromBody] Delta<RamoAtividade> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarRamoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Clientes@RamosAtividade({key})")]
    public async Task<ActionResult> AtualizarRamo(long key, [FromBody] Delta<RamoAtividade> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarRamoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Clientes@RamosAtividade({key})")]
    public async Task<ActionResult> RemoverRamo(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverRamoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Clientes@Portes")]
    public IQueryable<PorteEmpresa> ObterPortes() => servico.ObterPortes();

    [HttpPost("odata/Clientes@Portes")]
    public async Task<ActionResult> CriarPorte([FromBody] Delta<PorteEmpresa> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarPorteAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Clientes@Portes({key})")]
    public async Task<ActionResult> AtualizarPorte(long key, [FromBody] Delta<PorteEmpresa> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarPorteAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Clientes@Portes({key})")]
    public async Task<ActionResult> RemoverPorte(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverPorteAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

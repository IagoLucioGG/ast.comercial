using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class FormulariosController(IFormularioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Formulario> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Formulario>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Formulario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Formulario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Formularios@Secoes")]
    public IQueryable<SecaoFormulario> ObterSecoes() => servico.ObterSecoes();

    [HttpPost("odata/Formularios@Secoes")]
    public async Task<ActionResult> CriarSecao([FromBody] Delta<SecaoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarSecaoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Formularios@Secoes({key})")]
    public async Task<ActionResult> AtualizarSecao(long key, [FromBody] Delta<SecaoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarSecaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Formularios@Secoes({key})")]
    public async Task<ActionResult> RemoverSecao(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverSecaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Formularios@Campos")]
    public IQueryable<CampoFormulario> ObterCampos() => servico.ObterCampos();

    [HttpPost("odata/Formularios@Campos")]
    public async Task<ActionResult> CriarCampo([FromBody] Delta<CampoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarCampoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Formularios@Campos({key})")]
    public async Task<ActionResult> AtualizarCampo(long key, [FromBody] Delta<CampoFormulario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarCampoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Formularios@Campos({key})")]
    public async Task<ActionResult> RemoverCampo(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverCampoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

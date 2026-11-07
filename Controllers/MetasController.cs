using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class MetasController(IMetaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Meta> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Meta>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Meta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Meta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    /// <summary>
    /// Progresso das metas ativas no período (sobreposição com o período da meta).
    /// Sem inicio/fim: mês corrente (UTC). escopo = usuario | equipe | empresa (opcional).
    /// </summary>
    [HttpGet("api/metas/progresso")]
    public async Task<ActionResult> Progresso([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim,
        [FromQuery] long? usuarioId, [FromQuery] long? equipeId, [FromQuery] string? escopo, CancellationToken ct)
    {
        var lista = await servico.ObterProgressoAsync(inicio, fim, usuarioId, equipeId, escopo, ct);
        return Ok(lista);
    }
}

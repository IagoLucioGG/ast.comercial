using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class ModelosDocumentoController(IModeloDocumentoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ModeloDocumento> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<ModeloDocumento>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<ModeloDocumento> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ModeloDocumento> delta, CancellationToken ct)
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
    /// Renderiza o modelo com dados reais da proposta, retornando HTML pronto.
    /// </summary>
    [HttpGet("odata/ModelosDocumento({modeloId})/Renderizar({registroId})")]
    public async Task<ActionResult> Renderizar(long modeloId, long registroId, CancellationToken ct)
    {
        var html = await servico.RenderizarAsync(modeloId, registroId, ct);
        if (string.IsNullOrEmpty(html)) return NotFound();
        return Content(html, "text/html");
    }
}

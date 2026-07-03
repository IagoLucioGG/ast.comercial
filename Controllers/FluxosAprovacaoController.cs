using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class FluxosAprovacaoController(IFluxoAprovacaoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<FluxoAprovacao> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<FluxoAprovacao>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<FluxoAprovacao> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<FluxoAprovacao> delta, CancellationToken ct)
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
    [HttpGet("odata/FluxosAprovacao@Niveis")]
    public IQueryable<NivelAprovacao> ObterNiveis() => servico.ObterNiveis();

    [EnableQuery]
    [HttpGet("odata/FluxosAprovacao@Solicitacoes")]
    public IQueryable<SolicitacaoAprovacao> ObterSolicitacoes() => servico.ObterSolicitacoes();

    [HttpPost("odata/FluxosAprovacao@Solicitacoes({key})/Aprovar")]
    public async Task<ActionResult> Aprovar(long key, [FromBody] AprovacaoRequest? request, CancellationToken ct)
    {
        try
        {
            await servico.AprovarSolicitacaoAsync(key, request?.Observacao, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("odata/FluxosAprovacao@Solicitacoes({key})/Rejeitar")]
    public async Task<ActionResult> Rejeitar(long key, [FromBody] AprovacaoRequest? request, CancellationToken ct)
    {
        try
        {
            await servico.RejeitarSolicitacaoAsync(key, request?.Observacao, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("odata/FluxosAprovacao@Tipos")]
    public ActionResult ObterTipos()
    {
        var valores = Enum.GetNames<TipoFluxoAprovacao>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

public record AprovacaoRequest(string? Observacao);

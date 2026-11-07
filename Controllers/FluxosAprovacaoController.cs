using AST.Comercial.Application;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

/// <summary>Converte AcessoNegadoException (aprovador errado) em 403 { erro, status } com a mensagem legível.</summary>
public class AcessoNegadoAprovacaoFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not AcessoNegadoException ex) return;
        context.Result = new JsonResult(new { erro = ex.Message, status = 403 }) { StatusCode = 403 };
        context.ExceptionHandled = true;
    }
}

[AcessoNegadoAprovacaoFilter]
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

    /// <summary>Aprova o nível atual. 403 se o usuário não é aprovador do nível (Administrador pode e fica registrado).</summary>
    [HttpPost("odata/FluxosAprovacao@Solicitacoes({key})/Aprovar")]
    public async Task<ActionResult> Aprovar(long key, [FromBody] AprovacaoRequest? request, CancellationToken ct)
    {
        try
        {
            await servico.AprovarSolicitacaoAsync(key, request?.Comentario ?? request?.Observacao, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Rejeita e encerra a solicitação. Comentário obrigatório (422).</summary>
    [HttpPost("odata/FluxosAprovacao@Solicitacoes({key})/Rejeitar")]
    public async Task<ActionResult> Rejeitar(long key, [FromBody] AprovacaoRequest? request, CancellationToken ct)
    {
        try
        {
            await servico.RejeitarSolicitacaoAsync(key, request?.Comentario ?? request?.Observacao, ct);
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

public record AprovacaoRequest(string? Observacao, string? Comentario = null);

/// <summary>
/// CRUD completo de fluxos de aprovação de propostas (fluxo + filtros + níveis ordenados + aprovadores).
/// JSON em camelCase. Detalhes e campos de filtro suportados: mapeamento/analise/12-aprovacoes-onda3.md.
/// </summary>
[ApiController]
[Route("api/fluxos-aprovacao")]
[AcessoNegadoAprovacaoFilter]
public class FluxosAprovacaoApiController(IFluxoAprovacaoServico servico) : ControllerBase
{
    private ObjectResult Erro(int status, string mensagem) => StatusCode(status, new { erro = mensagem, status });

    [HttpGet]
    public async Task<ActionResult> Listar(CancellationToken ct) => Ok(await servico.ListarFluxosAsync(ct));

    /// <summary>Campos e operadores que podem ser usados nos filtros.</summary>
    [HttpGet("campos")]
    public ActionResult Campos() => Ok(servico.CamposFiltro());

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Obter(long id, CancellationToken ct)
    {
        var f = await servico.ObterFluxoAsync(id, ct);
        return f is null ? Erro(404, "Fluxo não encontrado.") : Ok(f);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] FluxoAprovacaoDto? corpo, CancellationToken ct)
    {
        if (corpo is null) return Erro(400, "Corpo da requisição inválido.");
        var criado = await servico.CriarFluxoAsync(corpo, ct);
        return StatusCode(201, criado);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] FluxoAprovacaoDto? corpo, CancellationToken ct)
    {
        if (corpo is null) return Erro(400, "Corpo da requisição inválido.");
        var f = await servico.AtualizarFluxoAsync(id, corpo, ct);
        return f is null ? Erro(404, "Fluxo não encontrado.") : Ok(f);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Excluir(long id, CancellationToken ct)
        => await servico.ExcluirFluxoAsync(id, ct) ? NoContent() : Erro(404, "Fluxo não encontrado.");
}

/// <summary>Caixa de aprovações: pendentes do usuário, solicitadas por ele e histórico. JSON em camelCase.</summary>
[ApiController]
[Route("api/aprovacoes")]
[AcessoNegadoAprovacaoFilter]
public class AprovacoesApiController(IFluxoAprovacaoServico servico) : ControllerBase
{
    private ObjectResult Erro(int status, string mensagem) => StatusCode(status, new { erro = mensagem, status });

    /// <summary>Solicitações em andamento em que o usuário logado é aprovador do nível atual.</summary>
    [HttpGet("minhas")]
    public async Task<ActionResult> Minhas(CancellationToken ct) => Ok(await servico.MinhasAsync(ct));

    /// <summary>Solicitações que o usuário logado pediu (qualquer situação).</summary>
    [HttpGet("solicitadas")]
    public async Task<ActionResult> Solicitadas(CancellationToken ct) => Ok(await servico.SolicitadasPorMimAsync(ct));

    /// <summary>Com entidade=Proposta&amp;id=N: todas as solicitações da proposta (mais recente primeiro). Sem id: concluídas/canceladas.</summary>
    [HttpGet("historico")]
    public async Task<ActionResult> Historico([FromQuery] string? entidade, [FromQuery] long? id, CancellationToken ct)
        => Ok(await servico.HistoricoAsync(entidade, id, ct));

    [HttpPost("{id:long}/aprovar")]
    public async Task<ActionResult> Aprovar(long id, [FromBody] DecisaoAprovacaoRequest? corpo, CancellationToken ct)
    {
        try { return Ok(await servico.DecidirSolicitacaoAsync(id, true, corpo?.Comentario ?? corpo?.Observacao, ct)); }
        catch (KeyNotFoundException) { return Erro(404, "Solicitação não encontrada."); }
    }

    [HttpPost("{id:long}/rejeitar")]
    public async Task<ActionResult> Rejeitar(long id, [FromBody] DecisaoAprovacaoRequest? corpo, CancellationToken ct)
    {
        try { return Ok(await servico.DecidirSolicitacaoAsync(id, false, corpo?.Comentario ?? corpo?.Observacao, ct)); }
        catch (KeyNotFoundException) { return Erro(404, "Solicitação não encontrada."); }
    }
}

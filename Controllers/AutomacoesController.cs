using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class AutomacoesController(IAutomacaoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Automacao> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Automacao>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [EnableQuery]
    [HttpGet("odata/Automacoes@Fila")]
    public IQueryable<FilaAutomacao> ObterFila() => servico.ObterFila();

    [EnableQuery]
    [HttpGet("odata/Automacoes@Logs")]
    public IQueryable<ExecucaoAutomacao> ObterLogs() => servico.ObterLogs();

    [HttpGet("odata/Automacoes@Gatilhos")]
    public ActionResult ObterGatilhos()
    {
        var valores = Enum.GetNames<TipoGatilho>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    [HttpGet("odata/Automacoes@Acoes")]
    public ActionResult ObterAcoes()
    {
        var valores = Enum.GetNames<TipoAcao>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

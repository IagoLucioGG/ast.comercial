using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class AtividadesController(IAtividadeServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<AtividadeDto> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<AtividadeDto>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult<AtividadeDto>> Post([FromBody] Delta<Atividade> delta)
    {
        var resultado = await servico.CriarAsync(delta);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Atividade> delta)
    {
        var resultado = await servico.AtualizarAsync(key, delta);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPost("odata/Atividades({key})/Concluir")]
    public async Task<ActionResult> Concluir(long key)
    {
        var concluida = await servico.ConcluirAsync(key);
        return concluida ? NoContent() : NotFound();
    }

    public async Task<ActionResult> Delete(long key)
    {
        var removido = await servico.RemoverAsync(key);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Atividades@Tipos")]
    public IQueryable<object> ObterTipos() => servico.ObterTipos();
}

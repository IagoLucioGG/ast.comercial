using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class AtividadesController(IAtividadeServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Atividade> Get() => servico.ObterTodos();

    [EnableQuery]
    // SingleResult (e não a entidade já materializada) para o [EnableQuery] aplicar $expand/$select no registro único.
    public SingleResult<Atividade> Get(long key) => SingleResult.Create(servico.ObterTodos().Where(a => a.Id == key));

    public async Task<ActionResult<Atividade>> Post([FromBody] Delta<Atividade> delta)
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

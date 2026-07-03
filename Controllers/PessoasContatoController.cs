using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class PessoasContatoController(IPessoaContatoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<PessoaContato> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<PessoaContato>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<PessoaContato> delta)
    {
        var resultado = await servico.CriarAsync(delta);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<PessoaContato> delta)
    {
        var resultado = await servico.AtualizarAsync(key, delta);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key)
    {
        var removido = await servico.RemoverAsync(key);
        return removido ? NoContent() : NotFound();
    }
}

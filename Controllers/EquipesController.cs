using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class EquipesController(IEquipeServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Equipe> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Equipe>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [EnableQuery]
    [HttpGet("odata/Equipes@Membros")]
    public IQueryable<MembroEquipe> ObterMembros() => servico.ObterMembros();

    [HttpPost("odata/Equipes@Membros")]
    public async Task<ActionResult> CriarMembro([FromBody] Delta<MembroEquipe> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarMembroAsync(delta, ct);
        return Created(resultado);
    }

    [HttpDelete("odata/Equipes@Membros({key})")]
    public async Task<ActionResult> RemoverMembro(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverMembroAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }
}

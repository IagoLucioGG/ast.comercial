using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class CargosController(ICargoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Cargo> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Cargo>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}

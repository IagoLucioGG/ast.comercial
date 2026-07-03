using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class EmpresasController(IEmpresaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Empresa> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Empresa>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}

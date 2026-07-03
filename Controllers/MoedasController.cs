using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class MoedasController(IMoedaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Moeda> Get() => servico.ObterTodos();
}

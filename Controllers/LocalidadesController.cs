using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class LocalidadesController(ILocalidadeServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Localidade> Get() => servico.ObterTodos();
}

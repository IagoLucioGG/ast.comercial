using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface ILocalidadeServico
{
    IQueryable<Localidade> ObterTodos();
}

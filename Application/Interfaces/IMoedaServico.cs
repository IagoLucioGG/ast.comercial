using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface IMoedaServico
{
    IQueryable<Moeda> ObterTodos();
}

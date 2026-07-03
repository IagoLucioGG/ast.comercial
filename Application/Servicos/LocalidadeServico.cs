using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class LocalidadeServico(AppDbContext db) : ILocalidadeServico
{
    public IQueryable<Localidade> ObterTodos()
        => db.Localidades.AsNoTracking();
}

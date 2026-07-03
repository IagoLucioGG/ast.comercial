using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class MoedaServico(AppDbContext db) : IMoedaServico
{
    public IQueryable<Moeda> ObterTodos()
        => db.Moedas.AsNoTracking();
}

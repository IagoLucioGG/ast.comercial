using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class MoedaServico(AppDbContext db) : IMoedaServico
{
    // O catálogo de moedas é global (seed com EmpresaId = 0). O filtro de tenant padrão
    // só devolveria moedas da empresa logada, então a lista vinha sempre vazia.
    // Aqui aceitamos o catálogo global e as moedas cadastradas pela própria empresa.
    public IQueryable<Moeda> ObterTodos()
        => db.Moedas.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.Ativo && (m.EmpresaId == 0 || m.EmpresaId == db.EmpresaIdAtual));
}

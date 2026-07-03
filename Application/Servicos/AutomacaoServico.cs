using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class AutomacaoServico(AppDbContext db) : IAutomacaoServico
{
    public IQueryable<Automacao> ObterTodos()
        => db.Automacoes.AsNoTracking();

    public async Task<Automacao?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Automacoes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public IQueryable<FilaAutomacao> ObterFila()
        => db.FilaAutomacao.AsNoTracking();

    public IQueryable<ExecucaoAutomacao> ObterLogs()
        => db.ExecucoesAutomacao.AsNoTracking();
}

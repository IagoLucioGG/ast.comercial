using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface IAutomacaoServico
{
    IQueryable<Automacao> ObterTodos();
    Task<Automacao?> ObterPorIdAsync(long id, CancellationToken ct = default);
    IQueryable<FilaAutomacao> ObterFila();
    IQueryable<ExecucaoAutomacao> ObterLogs();
}

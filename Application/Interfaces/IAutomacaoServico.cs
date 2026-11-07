using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

public interface IAutomacaoServico
{
    // Leitura OData (já filtrada pela empresa atual)
    IQueryable<Automacao> ObterTodos();
    Task<Automacao?> ObterPorIdAsync(long id, CancellationToken ct = default);
    IQueryable<FilaAutomacao> ObterFila();
    IQueryable<ExecucaoAutomacao> ObterLogs();

    // API api/automacoes
    Task<ListaPaginadaDto<AutomacaoDto>> ListarAsync(string? entidadeAlvo, string? gatilho, bool? ativo, string? busca, int skip, int top, CancellationToken ct = default);
    Task<AutomacaoDto?> ObterDtoAsync(long id, CancellationToken ct = default);
    Task<AutomacaoDto> CriarAsync(AutomacaoEntradaDto entrada, CancellationToken ct = default);
    Task<AutomacaoDto?> AtualizarAsync(long id, AutomacaoEntradaDto entrada, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task<AutomacaoDto?> DefinirAtivoAsync(long id, bool ativo, CancellationToken ct = default);
    Task<ListaPaginadaDto<ExecucaoAutomacaoDto>?> ListarExecucoesAsync(long id, string? status, int skip, int top, CancellationToken ct = default);
}

/// <summary>Falha de validação de uma automação (mapeada para HTTP 422 pelo controller).</summary>
public class ValidacaoAutomacaoException(IReadOnlyList<string> erros) : Exception(erros.Count > 0 ? erros[0] : "Automação inválida.")
{
    public IReadOnlyList<string> Erros { get; } = erros;
}

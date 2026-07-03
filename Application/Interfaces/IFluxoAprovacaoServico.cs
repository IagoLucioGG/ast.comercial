using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IFluxoAprovacaoServico
{
    IQueryable<FluxoAprovacao> ObterTodos();
    Task<FluxoAprovacao?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<FluxoAprovacao> CriarAsync(Delta<FluxoAprovacao> delta, CancellationToken ct = default);
    Task<FluxoAprovacao?> AtualizarAsync(long id, Delta<FluxoAprovacao> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    IQueryable<NivelAprovacao> ObterNiveis();
    IQueryable<SolicitacaoAprovacao> ObterSolicitacoes();
    Task AprovarSolicitacaoAsync(long solicitacaoId, string? observacao, CancellationToken ct = default);
    Task RejeitarSolicitacaoAsync(long solicitacaoId, string? observacao, CancellationToken ct = default);
}

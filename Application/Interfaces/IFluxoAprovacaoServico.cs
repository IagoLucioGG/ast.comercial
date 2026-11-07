using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IFluxoAprovacaoServico
{
    // ---- OData (compatível com a versão anterior) ----
    IQueryable<FluxoAprovacao> ObterTodos();
    Task<FluxoAprovacao?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<FluxoAprovacao> CriarAsync(Delta<FluxoAprovacao> delta, CancellationToken ct = default);
    Task<FluxoAprovacao?> AtualizarAsync(long id, Delta<FluxoAprovacao> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    IQueryable<NivelAprovacao> ObterNiveis();
    IQueryable<SolicitacaoAprovacao> ObterSolicitacoes();
    Task AprovarSolicitacaoAsync(long solicitacaoId, string? comentario, CancellationToken ct = default);
    Task RejeitarSolicitacaoAsync(long solicitacaoId, string? comentario, CancellationToken ct = default);

    // ---- CRUD completo de fluxos (api/fluxos-aprovacao) ----
    Task<List<FluxoAprovacaoDto>> ListarFluxosAsync(CancellationToken ct = default);
    Task<FluxoAprovacaoDto?> ObterFluxoAsync(long id, CancellationToken ct = default);
    Task<FluxoAprovacaoDto> CriarFluxoAsync(FluxoAprovacaoDto entrada, CancellationToken ct = default);
    Task<FluxoAprovacaoDto?> AtualizarFluxoAsync(long id, FluxoAprovacaoDto entrada, CancellationToken ct = default);
    /// <summary>Exclui de vez se nunca foi usado; com histórico de solicitações responde 422 (desative em vez de excluir).</summary>
    Task<bool> ExcluirFluxoAsync(long id, CancellationToken ct = default);
    IReadOnlyList<CampoFiltroAprovacaoDto> CamposFiltro();

    // ---- Motor de aprovação de propostas ----
    /// <summary>Avaliação automática ao salvar. Cria/cancela/mantém a solicitação conforme os fluxos ativos.</summary>
    Task AvaliarPropostaAsync(long propostaId, CancellationToken ct = default);
    /// <summary>Pedido explícito. 422 se nenhum fluxo se aplicar ou se a proposta já estiver aprovada/aguardando.</summary>
    Task<SolicitacaoAprovacaoDto> SolicitarAprovacaoPropostaAsync(long propostaId, string? observacao, CancellationToken ct = default);
    /// <summary>A proposta está sob controle de um fluxo (solicitação em andamento ou fluxo aplicável)?</summary>
    Task<bool> PropostaGovernadaPorFluxoAsync(long propostaId, CancellationToken ct = default);
    /// <summary>
    /// Decide a solicitação em andamento da proposta (ou cria uma, se um fluxo se aplica). Devolve false quando não
    /// há fluxo aplicável: o chamador mantém o comportamento anterior (aprovar/rejeitar direto).
    /// </summary>
    Task<bool> DecidirPropostaAsync(long propostaId, bool aprovar, string? comentario, CancellationToken ct = default);
    Task<SolicitacaoAprovacaoDto> DecidirSolicitacaoAsync(long solicitacaoId, bool aprovar, string? comentario, CancellationToken ct = default);

    // ---- Consultas ----
    Task<List<SolicitacaoAprovacaoDto>> MinhasAsync(CancellationToken ct = default);
    Task<List<SolicitacaoAprovacaoDto>> SolicitadasPorMimAsync(CancellationToken ct = default);
    Task<List<SolicitacaoAprovacaoDto>> HistoricoAsync(string? entidade, long? id, CancellationToken ct = default);
}

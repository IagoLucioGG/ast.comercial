using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IIntegracaoServico
{
    IQueryable<Integracao> ObterTodos();
    Task<Integracao?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Integracao> CriarAsync(Delta<Integracao> delta, CancellationToken ct = default);
    Task<Integracao?> AtualizarAsync(long id, Delta<Integracao> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task<Integracao?> ObterPorChaveWebhookAsync(string chave, CancellationToken ct = default);

    // Mapeamentos
    IQueryable<MapeamentoCampoIntegracao> ObterMapeamentos();
    Task<MapeamentoCampoIntegracao> CriarMapeamentoAsync(Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct = default);
    Task<MapeamentoCampoIntegracao?> AtualizarMapeamentoAsync(long id, Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct = default);
    Task<bool> RemoverMapeamentoAsync(long id, CancellationToken ct = default);

    // Fluxos
    IQueryable<FluxoIntegracao> ObterFluxos();
    Task<FluxoIntegracao> CriarFluxoAsync(Delta<FluxoIntegracao> delta, CancellationToken ct = default);
    Task<FluxoIntegracao?> AtualizarFluxoAsync(long id, Delta<FluxoIntegracao> delta, CancellationToken ct = default);
    Task<bool> RemoverFluxoAsync(long id, CancellationToken ct = default);

    // Etapas
    IQueryable<EtapaFluxoIntegracao> ObterEtapas();
    Task<EtapaFluxoIntegracao> CriarEtapaAsync(Delta<EtapaFluxoIntegracao> delta, CancellationToken ct = default);
    Task<EtapaFluxoIntegracao?> AtualizarEtapaAsync(long id, Delta<EtapaFluxoIntegracao> delta, CancellationToken ct = default);
    Task<bool> RemoverEtapaAsync(long id, CancellationToken ct = default);

    // Logs
    IQueryable<LogIntegracao> ObterLogs();
}

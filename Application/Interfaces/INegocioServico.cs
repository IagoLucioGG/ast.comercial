using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface INegocioServico
{
    IQueryable<Negocio> ObterTodos();
    Task<Negocio?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<NegocioDto> CriarAsync(Delta<Negocio> delta, CancellationToken ct = default);
    Task<NegocioDto?> AtualizarAsync(long id, Delta<Negocio> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    Task GanharAsync(long id, CancellationToken ct = default);
    Task PerderAsync(long id, CancellationToken ct = default);
    Task ReabrirAsync(long id, CancellationToken ct = default);
    IQueryable<StatusNegocio> ObterStatus();
    Task<StatusNegocio> CriarStatusAsync(Delta<StatusNegocio> delta, CancellationToken ct = default);
    Task<StatusNegocio?> AtualizarStatusAsync(long id, Delta<StatusNegocio> delta, CancellationToken ct = default);
    Task<bool> RemoverStatusAsync(long id, CancellationToken ct = default);

    IQueryable<MotivoPerda> ObterMotivosPerda();
    Task<MotivoPerda> CriarMotivoPerdaAsync(Delta<MotivoPerda> delta, CancellationToken ct = default);
    Task<MotivoPerda?> AtualizarMotivoPerdaAsync(long id, Delta<MotivoPerda> delta, CancellationToken ct = default);
    Task<bool> RemoverMotivoPerdaAsync(long id, CancellationToken ct = default);

    IQueryable<Funil> ObterFunis();
    IQueryable<EtapaFunil> ObterEtapas();
}

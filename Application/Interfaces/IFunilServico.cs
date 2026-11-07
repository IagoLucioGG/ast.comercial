using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IFunilServico
{
    IQueryable<Funil> ObterTodos();
    Task<Funil?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Funil> CriarAsync(Delta<Funil> delta, CancellationToken cancellationToken = default);
    Task<Funil?> AtualizarAsync(long id, Delta<Funil> delta, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default);
    IQueryable<EtapaFunil> ObterEtapas(long funilId);
    Task<EtapaFunil> CriarEtapaAsync(long funilId, Delta<EtapaFunil> delta, CancellationToken cancellationToken = default);
}

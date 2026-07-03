using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IFunilServico
{
    IQueryable<FunilDto> ObterTodos();
    Task<FunilDto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<FunilDto> CriarAsync(Delta<Funil> delta, CancellationToken cancellationToken = default);
    Task<FunilDto?> AtualizarAsync(long id, Delta<Funil> delta, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default);
    IQueryable<EtapaFunilDto> ObterEtapas(long funilId);
    Task<EtapaFunilDto> CriarEtapaAsync(Delta<EtapaFunil> delta, CancellationToken cancellationToken = default);
}

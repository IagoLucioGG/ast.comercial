using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IClienteServico
{
    IQueryable<Cliente> ObterTodos();
    Task<Cliente?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<ClienteDto> CriarAsync(Delta<Cliente> delta, CancellationToken ct = default);
    Task<ClienteDto?> AtualizarAsync(long id, Delta<Cliente> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    IQueryable<OrigemContato> ObterOrigens();
    Task<OrigemContato> CriarOrigemAsync(Delta<OrigemContato> delta, CancellationToken ct = default);
    Task<OrigemContato?> AtualizarOrigemAsync(long id, Delta<OrigemContato> delta, CancellationToken ct = default);
    Task<bool> RemoverOrigemAsync(long id, CancellationToken ct = default);

    IQueryable<StatusCliente> ObterStatusClientes();
    Task<StatusCliente> CriarStatusAsync(Delta<StatusCliente> delta, CancellationToken ct = default);
    Task<StatusCliente?> AtualizarStatusAsync(long id, Delta<StatusCliente> delta, CancellationToken ct = default);
    Task<bool> RemoverStatusAsync(long id, CancellationToken ct = default);

    IQueryable<RamoAtividade> ObterRamos();
    Task<RamoAtividade> CriarRamoAsync(Delta<RamoAtividade> delta, CancellationToken ct = default);
    Task<RamoAtividade?> AtualizarRamoAsync(long id, Delta<RamoAtividade> delta, CancellationToken ct = default);
    Task<bool> RemoverRamoAsync(long id, CancellationToken ct = default);

    IQueryable<PorteEmpresa> ObterPortes();
    Task<PorteEmpresa> CriarPorteAsync(Delta<PorteEmpresa> delta, CancellationToken ct = default);
    Task<PorteEmpresa?> AtualizarPorteAsync(long id, Delta<PorteEmpresa> delta, CancellationToken ct = default);
    Task<bool> RemoverPorteAsync(long id, CancellationToken ct = default);
}

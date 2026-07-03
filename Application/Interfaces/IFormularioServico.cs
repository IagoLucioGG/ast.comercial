using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IFormularioServico
{
    IQueryable<Formulario> ObterTodos();
    Task<Formulario?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Formulario> CriarAsync(Delta<Formulario> delta, CancellationToken ct = default);
    Task<Formulario?> AtualizarAsync(long id, Delta<Formulario> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);
    IQueryable<SecaoFormulario> ObterSecoes();
    Task<SecaoFormulario> CriarSecaoAsync(Delta<SecaoFormulario> delta, CancellationToken ct = default);
    Task<SecaoFormulario?> AtualizarSecaoAsync(long id, Delta<SecaoFormulario> delta, CancellationToken ct = default);
    Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default);
    IQueryable<CampoFormulario> ObterCampos();
    Task<CampoFormulario> CriarCampoAsync(Delta<CampoFormulario> delta, CancellationToken ct = default);
    Task<CampoFormulario?> AtualizarCampoAsync(long id, Delta<CampoFormulario> delta, CancellationToken ct = default);
    Task<bool> RemoverCampoAsync(long id, CancellationToken ct = default);
}

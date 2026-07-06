using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IModeloDocumentoServico
{
    IQueryable<ModeloDocumento> ObterTodos();
    Task<ModeloDocumento?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<ModeloDocumento> CriarAsync(Delta<ModeloDocumento> delta, CancellationToken ct = default);
    Task<ModeloDocumento?> AtualizarAsync(long id, Delta<ModeloDocumento> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    IQueryable<SecaoModelo> ObterSecoes();
    Task<SecaoModelo> CriarSecaoAsync(Delta<SecaoModelo> delta, CancellationToken ct = default);
    Task<SecaoModelo?> AtualizarSecaoAsync(long id, Delta<SecaoModelo> delta, CancellationToken ct = default);
    Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Renderiza o documento substituindo placeholders pelos valores reais.
    /// </summary>
    Task<string> RenderizarAsync(long modeloId, long registroId, CancellationToken ct = default);
}

using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface IEmpresaServico
{
    IQueryable<Empresa> ObterTodos();
    Task<Empresa?> ObterPorIdAsync(long id, CancellationToken ct = default);
    /// <summary>Atualiza a empresa do usuário logado. Retorna null se o id não for a empresa atual.</summary>
    Task<Empresa?> AtualizarAsync(long id, Delta<Empresa> delta, CancellationToken ct = default);
}

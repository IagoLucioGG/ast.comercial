using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public interface ICampoServico
{
    IQueryable<Campo> ObterTodos();
    Task<Campo?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Campo> CriarAsync(Delta<Campo> delta, CancellationToken ct = default);
    Task<Campo?> AtualizarAsync(long id, Delta<Campo> delta, CancellationToken ct = default);
    Task<bool> RemoverAsync(long id, CancellationToken ct = default);

    IQueryable<OpcaoCampo> ObterOpcoes();
    Task<OpcaoCampo> CriarOpcaoAsync(Delta<OpcaoCampo> delta, CancellationToken ct = default);
    Task<OpcaoCampo?> AtualizarOpcaoAsync(long id, Delta<OpcaoCampo> delta, CancellationToken ct = default);
    Task<bool> RemoverOpcaoAsync(long id, CancellationToken ct = default);

    IQueryable<RegraCampo> ObterRegras();
    Task<RegraCampo> CriarRegraAsync(Delta<RegraCampo> delta, CancellationToken ct = default);
    Task<RegraCampo?> AtualizarRegraAsync(long id, Delta<RegraCampo> delta, CancellationToken ct = default);
    Task<bool> RemoverRegraAsync(long id, CancellationToken ct = default);

    IQueryable<ConfiguracaoCampo> ObterConfiguracoes();
    Task<ConfiguracaoCampo> CriarConfiguracaoAsync(Delta<ConfiguracaoCampo> delta, CancellationToken ct = default);
    Task<ConfiguracaoCampo?> AtualizarConfiguracaoAsync(long id, Delta<ConfiguracaoCampo> delta, CancellationToken ct = default);
    Task<bool> RemoverConfiguracaoAsync(long id, CancellationToken ct = default);

    IQueryable<ValorCampo> ObterValores();
    Task<ValorCampo> CriarValorAsync(Delta<ValorCampo> delta, CancellationToken ct = default);
    Task<ValorCampo?> AtualizarValorAsync(long id, Delta<ValorCampo> delta, CancellationToken ct = default);
    Task<bool> RemoverValorAsync(long id, CancellationToken ct = default);
}

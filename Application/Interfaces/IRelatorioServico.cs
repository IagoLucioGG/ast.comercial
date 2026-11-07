using AST.Comercial.Application.Dtos;
namespace AST.Comercial.Application.Interfaces;

/// <summary>Usuário que faz a chamada (para visibilidade de painéis).</summary>
public record UsuarioRelatorio(long? Id, bool Administrador);

/// <summary>Acesso negado a um painel (mapeado para HTTP 403 com mensagem).</summary>
public class AcessoRelatorioNegadoException(string mensagem) : Exception(mensagem);

public interface IRelatorioServico
{
    CatalogoRelatorioDto ObterCatalogo();
    Task<List<OpcaoRelDto>> ObterOpcoesAsync(string referencia, string? busca, CancellationToken ct);
    Task<RespostaDadosDto> ConsultarDadosAsync(ConsultaDadosDto consulta, CancellationToken ct);

    Task<List<PainelSaidaDto>> ListarPaineisAsync(UsuarioRelatorio usuario, CancellationToken ct);
    Task<PainelSaidaDto?> ObterPainelAsync(long id, UsuarioRelatorio usuario, CancellationToken ct);
    Task<PainelSaidaDto> CriarPainelAsync(PainelEntradaDto entrada, UsuarioRelatorio usuario, CancellationToken ct);
    Task<PainelSaidaDto?> AtualizarPainelAsync(long id, PainelEntradaDto entrada, UsuarioRelatorio usuario, CancellationToken ct);
    Task<bool> ExcluirPainelAsync(long id, UsuarioRelatorio usuario, CancellationToken ct);

    Task<GraficoSaidaDto?> CriarGraficoAsync(long painelId, GraficoEntradaDto entrada, UsuarioRelatorio usuario, CancellationToken ct);
    Task<GraficoSaidaDto?> AtualizarGraficoAsync(long id, GraficoEntradaDto entrada, UsuarioRelatorio usuario, CancellationToken ct);
    Task<bool> ExcluirGraficoAsync(long id, UsuarioRelatorio usuario, CancellationToken ct);
    Task<bool> ReordenarGraficosAsync(long painelId, List<long> ids, UsuarioRelatorio usuario, CancellationToken ct);
}

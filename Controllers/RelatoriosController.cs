using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AST.Comercial.Controllers;

/// <summary>Painéis de relatório, gráficos e endpoint de agregação segura (lista branca por entidade).</summary>
[ApiController]
[Route("api/relatorios")]
public class RelatoriosController(IRelatorioServico servico) : ControllerBase
{
    private UsuarioRelatorio Usuario()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        long? id = long.TryParse(sub, out var v) ? v : null;
        return new UsuarioRelatorio(id, string.Equals(User.FindFirst("perfil")?.Value, "Administrador", StringComparison.OrdinalIgnoreCase));
    }

    private ObjectResult Negado(AcessoRelatorioNegadoException ex) => StatusCode(403, new { erro = ex.Message, status = 403 });

    [HttpGet("catalogo")]
    public ActionResult<CatalogoRelatorioDto> Catalogo() => servico.ObterCatalogo();

    [HttpGet("opcoes/{referencia}")]
    public async Task<ActionResult<List<OpcaoRelDto>>> Opcoes(string referencia, [FromQuery] string? busca, CancellationToken ct)
        => await servico.ObterOpcoesAsync(referencia, busca, ct);

    [HttpPost("dados")]
    public async Task<ActionResult<RespostaDadosDto>> Dados([FromBody] ConsultaDadosDto consulta, CancellationToken ct)
        => await servico.ConsultarDadosAsync(consulta, ct);

    [HttpGet("paineis")]
    public async Task<ActionResult<List<PainelSaidaDto>>> ListarPaineis(CancellationToken ct)
        => await servico.ListarPaineisAsync(Usuario(), ct);

    [HttpGet("paineis/{id:long}")]
    public async Task<ActionResult<PainelSaidaDto>> ObterPainel(long id, CancellationToken ct)
    {
        var p = await servico.ObterPainelAsync(id, Usuario(), ct);
        return p is null ? NotFound(new { erro = "Painel não encontrado.", status = 404 }) : p;
    }

    [HttpPost("paineis")]
    public async Task<ActionResult<PainelSaidaDto>> CriarPainel([FromBody] PainelEntradaDto entrada, CancellationToken ct)
        => await servico.CriarPainelAsync(entrada, Usuario(), ct);

    [HttpPut("paineis/{id:long}")]
    public async Task<ActionResult<PainelSaidaDto>> AtualizarPainel(long id, [FromBody] PainelEntradaDto entrada, CancellationToken ct)
    {
        try
        {
            var p = await servico.AtualizarPainelAsync(id, entrada, Usuario(), ct);
            return p is null ? NotFound(new { erro = "Painel não encontrado.", status = 404 }) : p;
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }

    [HttpDelete("paineis/{id:long}")]
    public async Task<ActionResult> ExcluirPainel(long id, CancellationToken ct)
    {
        try
        {
            return await servico.ExcluirPainelAsync(id, Usuario(), ct) ? NoContent() : NotFound(new { erro = "Painel não encontrado.", status = 404 });
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }

    [HttpPost("paineis/{painelId:long}/graficos")]
    public async Task<ActionResult<GraficoSaidaDto>> CriarGrafico(long painelId, [FromBody] GraficoEntradaDto entrada, CancellationToken ct)
    {
        try
        {
            var g = await servico.CriarGraficoAsync(painelId, entrada, Usuario(), ct);
            return g is null ? NotFound(new { erro = "Painel não encontrado.", status = 404 }) : g;
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }

    [HttpPost("paineis/{painelId:long}/graficos/reordenar")]
    public async Task<ActionResult> Reordenar(long painelId, [FromBody] ReordenarDto corpo, CancellationToken ct)
    {
        try
        {
            return await servico.ReordenarGraficosAsync(painelId, corpo.Ids, Usuario(), ct) ? NoContent() : NotFound(new { erro = "Painel não encontrado.", status = 404 });
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }

    [HttpPut("graficos/{id:long}")]
    public async Task<ActionResult<GraficoSaidaDto>> AtualizarGrafico(long id, [FromBody] GraficoEntradaDto entrada, CancellationToken ct)
    {
        try
        {
            var g = await servico.AtualizarGraficoAsync(id, entrada, Usuario(), ct);
            return g is null ? NotFound(new { erro = "Gráfico não encontrado.", status = 404 }) : g;
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }

    [HttpDelete("graficos/{id:long}")]
    public async Task<ActionResult> ExcluirGrafico(long id, CancellationToken ct)
    {
        try
        {
            return await servico.ExcluirGraficoAsync(id, Usuario(), ct) ? NoContent() : NotFound(new { erro = "Gráfico não encontrado.", status = 404 });
        }
        catch (AcessoRelatorioNegadoException ex) { return Negado(ex); }
    }
}

using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class CamposController(ICampoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Campo> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Campo>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Campo> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Campo> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Campos@Opcoes")]
    public IQueryable<OpcaoCampo> ObterOpcoes() => servico.ObterOpcoes();

    [HttpPost("odata/Campos@Opcoes")]
    public async Task<ActionResult> CriarOpcao([FromBody] Delta<OpcaoCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarOpcaoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Campos@Opcoes({key})")]
    public async Task<ActionResult> AtualizarOpcao(long key, [FromBody] Delta<OpcaoCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarOpcaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Campos@Opcoes({key})")]
    public async Task<ActionResult> RemoverOpcao(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverOpcaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Campos@Regras")]
    public IQueryable<RegraCampo> ObterRegras() => servico.ObterRegras();

    [HttpPost("odata/Campos@Regras")]
    public async Task<ActionResult> CriarRegra([FromBody] Delta<RegraCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarRegraAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Campos@Regras({key})")]
    public async Task<ActionResult> AtualizarRegra(long key, [FromBody] Delta<RegraCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarRegraAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Campos@Regras({key})")]
    public async Task<ActionResult> RemoverRegra(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverRegraAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Campos@Configuracoes")]
    public IQueryable<ConfiguracaoCampo> ObterConfiguracoes() => servico.ObterConfiguracoes();

    [HttpPost("odata/Campos@Configuracoes")]
    public async Task<ActionResult> CriarConfiguracao([FromBody] Delta<ConfiguracaoCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarConfiguracaoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Campos@Configuracoes({key})")]
    public async Task<ActionResult> AtualizarConfiguracao(long key, [FromBody] Delta<ConfiguracaoCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarConfiguracaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Campos@Configuracoes({key})")]
    public async Task<ActionResult> RemoverConfiguracao(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverConfiguracaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Campos@Valores")]
    public IQueryable<ValorCampo> ObterValores() => servico.ObterValores();

    [HttpPost("odata/Campos@Valores")]
    public async Task<ActionResult> CriarValor([FromBody] Delta<ValorCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarValorAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Campos@Valores({key})")]
    public async Task<ActionResult> AtualizarValor(long key, [FromBody] Delta<ValorCampo> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarValorAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Campos@Valores({key})")]
    public async Task<ActionResult> RemoverValor(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverValorAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpGet("odata/Campos@Entidades")]
    public ActionResult ObterEntidades()
    {
        var valores = Enum.GetNames<EntidadeAlvo>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    [HttpGet("odata/Campos@Tipos")]
    public ActionResult ObterTipos()
    {
        var valores = Enum.GetNames<TipoCampo>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    [HttpGet("odata/Campos@Esquema({entidade})")]
    public ActionResult ObterEsquema(string entidade)
    {
        if (!Enum.TryParse<EntidadeAlvo>(entidade, out var alvo))
            return BadRequest(new { erro = "EntidadeAlvo inválida." });

        var esquema = ContextoFormula.ObterEsquema(alvo);
        return Ok(esquema);
    }
}

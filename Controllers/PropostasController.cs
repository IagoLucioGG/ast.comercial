using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class PropostasController(IPropostaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Proposta> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Proposta>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Proposta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Proposta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpPost("odata/Propostas({key})/Aprovar")]
    public async Task<ActionResult> Aprovar(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.AprovarAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("odata/Propostas({key})/Rejeitar")]
    public async Task<ActionResult> Rejeitar(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.RejeitarAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Produtos")]
    public IQueryable<ItemProposta> ObterProdutos() => servico.ObterProdutos();

    [HttpPost("odata/Propostas@Produtos")]
    public async Task<ActionResult> CriarProduto([FromBody] Delta<ItemProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarProdutoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Propostas@Produtos({key})")]
    public async Task<ActionResult> AtualizarProduto(long key, [FromBody] Delta<ItemProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarProdutoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Propostas@Produtos({key})")]
    public async Task<ActionResult> RemoverProduto(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverProdutoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Secoes")]
    public IQueryable<SecaoProposta> ObterSecoes() => servico.ObterSecoes();

    [HttpPost("odata/Propostas@Secoes")]
    public async Task<ActionResult> CriarSecao([FromBody] Delta<SecaoProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarSecaoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Propostas@Secoes({key})")]
    public async Task<ActionResult> AtualizarSecao(long key, [FromBody] Delta<SecaoProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarSecaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Propostas@Secoes({key})")]
    public async Task<ActionResult> RemoverSecao(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverSecaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Parcelas")]
    public IQueryable<ParcelaProposta> ObterParcelas() => servico.ObterParcelas();

    [HttpPost("odata/Propostas@Parcelas")]
    public async Task<ActionResult> CriarParcela([FromBody] Delta<ParcelaProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarParcelaAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Propostas@Parcelas({key})")]
    public async Task<ActionResult> AtualizarParcela(long key, [FromBody] Delta<ParcelaProposta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarParcelaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Propostas@Parcelas({key})")]
    public async Task<ActionResult> RemoverParcela(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverParcelaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpGet("odata/Propostas@StatusAprovacao")]
    public ActionResult ObterStatusAprovacao()
    {
        var valores = Enum.GetNames<StatusAprovacaoProposta>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

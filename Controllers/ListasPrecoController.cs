using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class ListasPrecoController(IListaPrecoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ListaPreco> Get() => servico.ObterTodas();

    [EnableQuery]
    public async Task<ActionResult<ListaPreco>> Get(long key)
    {
        var r = await servico.ObterPorIdAsync(key);
        return r is null ? NotFound() : Ok(r);
    }

    public async Task<ActionResult> Post([FromBody] Delta<ListaPreco> delta, CancellationToken ct)
        => Created(await servico.CriarAsync(delta, ct));

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ListaPreco> delta, CancellationToken ct)
    {
        var r = await servico.AtualizarAsync(key, delta, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
        => await servico.RemoverAsync(key, ct) ? NoContent() : NotFound();
}

public class ItensListaPrecoController(IListaPrecoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ItemListaPreco> Get() => servico.ObterItens();

    [EnableQuery]
    public async Task<ActionResult<ItemListaPreco>> Get(long key)
    {
        var r = await servico.ObterItemPorIdAsync(key);
        return r is null ? NotFound() : Ok(r);
    }

    public async Task<ActionResult> Post([FromBody] Delta<ItemListaPreco> delta, CancellationToken ct)
        => Created(await servico.CriarItemAsync(delta, ct));

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ItemListaPreco> delta, CancellationToken ct)
    {
        var r = await servico.AtualizarItemAsync(key, delta, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
        => await servico.RemoverItemAsync(key, ct) ? NoContent() : NotFound();
}

/// <summary>Operações compostas de preço (fora do OData).</summary>
[ApiController]
public class PrecosController(IListaPrecoServico servico) : ControllerBase
{
    /// <summary>Preço base do produto e preço em cada lista vigente (todas=true inclui não vigentes e sem item).</summary>
    [HttpGet("api/produtos/{id:long}/precos")]
    public async Task<ActionResult<PrecosProdutoDto>> PrecosDoProduto(long id, [FromQuery] bool todas = false, CancellationToken ct = default)
    {
        var r = await servico.ObterPrecosProdutoAsync(id, todas, ct);
        return r is null ? NotFound(new { erro = "Produto não encontrado.", status = 404 }) : Ok(r);
    }

    /// <summary>Copia o preço base dos produtos para a lista (sobrescrever=true atualiza itens existentes).</summary>
    [HttpPost("api/listas-preco/{id:long}/copiar-precos-base")]
    public async Task<ActionResult<ResultadoCopiaPrecos>> CopiarPrecosBase(long id, [FromQuery] bool sobrescrever = false, CancellationToken ct = default)
        => Ok(await servico.CopiarPrecosBaseAsync(id, sobrescrever, ct));
}

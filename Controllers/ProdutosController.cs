using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class ProdutosController(IProdutoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Produto> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Produto>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Produto> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Produto> delta, CancellationToken ct)
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
    [HttpGet("odata/Produtos@Familias")]
    public IQueryable<FamiliaProduto> ObterFamilias() => servico.ObterFamilias();

    [HttpPost("odata/Produtos@Familias")]
    public async Task<ActionResult> CriarFamilia([FromBody] Delta<FamiliaProduto> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarFamiliaAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Produtos@Familias({key})")]
    public async Task<ActionResult> AtualizarFamilia(long key, [FromBody] Delta<FamiliaProduto> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarFamiliaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Produtos@Familias({key})")]
    public async Task<ActionResult> RemoverFamilia(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverFamiliaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Produtos@Grupos")]
    public IQueryable<GrupoProduto> ObterGrupos() => servico.ObterGrupos();

    [HttpPost("odata/Produtos@Grupos")]
    public async Task<ActionResult> CriarGrupo([FromBody] Delta<GrupoProduto> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarGrupoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Produtos@Grupos({key})")]
    public async Task<ActionResult> AtualizarGrupo(long key, [FromBody] Delta<GrupoProduto> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarGrupoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Produtos@Grupos({key})")]
    public async Task<ActionResult> RemoverGrupo(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverGrupoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpGet("odata/Produtos@Categorias")]
    public ActionResult ObterCategorias()
    {
        var valores = Enum.GetNames<CategoriaProduto>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

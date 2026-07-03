using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

public class VisualizacoesController(AppDbContext db) : ODataController
{
    [EnableQuery]
    public IQueryable<VisualizacaoSalva> Get()
        => db.VisualizacoesSalvas.AsNoTracking();

    [EnableQuery]
    public async Task<ActionResult<VisualizacaoSalva>> Get(long key)
    {
        var item = await db.VisualizacoesSalvas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == key);
        return item is null ? NotFound() : Ok(item);
    }

    public async Task<ActionResult> Post([FromBody] Delta<VisualizacaoSalva> delta, CancellationToken ct)
    {
        var entidade = new VisualizacaoSalva { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);

        var sub = User.FindFirst("sub")?.Value;
        if (long.TryParse(sub, out var uid))
            entidade.CriadoPorId = uid;

        db.VisualizacoesSalvas.Add(entidade);
        await db.SaveChangesAsync(ct);
        return Created(entidade);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<VisualizacaoSalva> delta, CancellationToken ct)
    {
        var entidade = await db.VisualizacoesSalvas.FirstOrDefaultAsync(v => v.Id == key, ct);
        if (entidade is null) return NotFound();

        delta.Patch(entidade);
        entidade.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(entidade);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var entidade = await db.VisualizacoesSalvas.FirstOrDefaultAsync(v => v.Id == key, ct);
        if (entidade is null) return NotFound();

        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

/// <summary>CRUD OData de checklists por etapa: odata/ChecklistsEtapa (use $expand=Itens).</summary>
public class ChecklistsEtapaController(IChecklistServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ChecklistEtapa> Get() => servico.ObterChecklists();

    [EnableQuery]
    public SingleResult<ChecklistEtapa> Get(long key) => SingleResult.Create(servico.ObterChecklists().Where(c => c.Id == key));

    public async Task<ActionResult> Post([FromBody] Delta<ChecklistEtapa> delta, CancellationToken ct)
        => Created(await servico.CriarChecklistAsync(delta, ct));

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ChecklistEtapa> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarChecklistAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
        => await servico.RemoverChecklistAsync(key, ct) ? NoContent() : NotFound();
}

/// <summary>CRUD OData dos itens de checklist: odata/ItensChecklistEtapa.</summary>
public class ItensChecklistEtapaController(IChecklistServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<ItemChecklistEtapa> Get() => servico.ObterItens();

    [EnableQuery]
    public SingleResult<ItemChecklistEtapa> Get(long key) => SingleResult.Create(servico.ObterItens().Where(i => i.Id == key));

    public async Task<ActionResult> Post([FromBody] Delta<ItemChecklistEtapa> delta, CancellationToken ct)
        => Created(await servico.CriarItemAsync(delta, ct));

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<ItemChecklistEtapa> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarItemAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
        => await servico.RemoverItemAsync(key, ct) ? NoContent() : NotFound();
}

/// <summary>Corpo de marcar/desmarcar item do checklist do negócio.</summary>
public record MarcarItemChecklistRequest(long ItemId, bool Concluido);

/// <summary>
/// Checklist aplicável a um negócio (rotas compostas; respostas em camelCase):
/// GET odata/Negocios({id})/Checklist · POST odata/Negocios({id})/MarcarItemChecklist { "ItemId": n, "Concluido": true|false } ·
/// GET odata/Funis({id})/ChecklistProgresso (uma consulta agregada para o kanban: negócios abertos nas etapas com checklist).
/// </summary>
public class ChecklistNegocioController(IChecklistServico servico) : ODataController
{
    [HttpGet("odata/Negocios({key})/Checklist")]
    public async Task<ActionResult> Obter(long key, CancellationToken ct)
        => Ok(await servico.ObterDoNegocioAsync(key, ct));

    [HttpPost("odata/Negocios({key})/MarcarItemChecklist")]
    public async Task<ActionResult> Marcar(long key, [FromBody] MarcarItemChecklistRequest? corpo, CancellationToken ct)
    {
        if (corpo is null || corpo.ItemId <= 0) return BadRequest(new { erro = "Informe ItemId e Concluido.", status = 400 });
        return Ok(await servico.MarcarItemAsync(key, corpo.ItemId, corpo.Concluido, ct));
    }

    [HttpGet("odata/Funis({key})/ChecklistProgresso")]
    public async Task<ActionResult> Progresso(long key, CancellationToken ct)
        => Ok(await servico.ObterProgressoPorFunilAsync(key, ct));
}

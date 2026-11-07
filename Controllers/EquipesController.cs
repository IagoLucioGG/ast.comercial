using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class EquipesController(IEquipeServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Equipe> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Equipe>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Equipe> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Equipe> delta, CancellationToken ct)
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
    [HttpGet("odata/Equipes@Membros")]
    public IQueryable<MembroEquipe> ObterMembros() => servico.ObterMembros();

    // As rotas "@" ficam fora do contexto OData: Delta não é preenchido ali, então usamos DTOs simples.
    [HttpPost("odata/Equipes@Membros")]
    public async Task<ActionResult> CriarMembro([FromBody] MembroEquipeRequest request, CancellationToken ct)
    {
        var papel = ParsePapel(request.Papel) ?? PapelEquipe.Membro;
        var m = await servico.CriarMembroAsync(request.EquipeId, request.UsuarioId, papel, ct);
        return StatusCode(201, Projetar(m));
    }

    [HttpPatch("odata/Equipes@Membros({key})")]
    public async Task<ActionResult> AtualizarMembro(long key, [FromBody] MembroEquipeRequest request, CancellationToken ct)
    {
        var papel = ParsePapel(request.Papel) ?? throw new AST.Comercial.Application.RegraNegocioException("Papel é obrigatório.");
        var m = await servico.AtualizarMembroAsync(key, papel, ct);
        return m is null ? NotFound() : Ok(Projetar(m));
    }

    [HttpDelete("odata/Equipes@Membros({key})")]
    public async Task<ActionResult> RemoverMembro(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverMembroAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    private static object Projetar(MembroEquipe m) => new { m.Id, m.EquipeId, m.UsuarioId, Papel = m.Papel.ToString(), m.Ativo };

    private static PapelEquipe? ParsePapel(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (Enum.TryParse<PapelEquipe>(valor.Trim(), true, out var p) && Enum.IsDefined(p)) return p;
        throw new AST.Comercial.Application.RegraNegocioException("Papel inválido. Use Lider ou Membro.");
    }
}

/// <summary>Papel: "Lider" (responsável) ou "Membro". Aceita também 0/1.</summary>
public record MembroEquipeRequest(long EquipeId, long UsuarioId, string? Papel);

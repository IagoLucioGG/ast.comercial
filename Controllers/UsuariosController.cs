using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class UsuariosController(IUsuarioServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Usuario> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Usuario>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Usuario> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Usuario> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpPost("odata/Usuarios({key})/RegenerarToken")]
    public async Task<ActionResult> RegenerarToken(long key, CancellationToken ct)
    {
        try
        {
            var token = await servico.RegenerarTokenIntegracaoAsync(key, ct);
            return Ok(new { TokenIntegracao = token });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("odata/Usuarios({key})/AlterarSenha")]
    public async Task<ActionResult> AlterarSenha(long key, [FromBody] AlterarSenhaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha))
            return BadRequest(new { erro = "Nova senha é obrigatória." });

        try
        {
            await servico.AlterarSenhaAsync(key, request.SenhaAtual ?? "", request.NovaSenha, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("odata/Usuarios@Perfis")]
    public ActionResult ObterPerfis()
    {
        var valores = Enum.GetNames<PerfilUsuario>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    [HttpGet("odata/Usuarios@Tipos")]
    public ActionResult ObterTipos()
    {
        var valores = Enum.GetNames<TipoUsuario>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

public record AlterarSenhaRequest(string? SenhaAtual, string NovaSenha);

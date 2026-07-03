using AST.Comercial.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AST.Comercial.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacaoController(IAutenticacaoServico servico) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<RespostaLoginDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { erro = "Email e senha são obrigatórios." });

        var origem = $"{HttpContext.Connection.RemoteIpAddress} | {Request.Headers.UserAgent}";
        var resultado = await servico.LoginAsync(request.Email, request.Senha, origem, cancellationToken);

        if (resultado is null)
        {
            // Delay intencional para dificultar brute force
            await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
            return Unauthorized(new { erro = "Credenciais inválidas." });
        }

        return Ok(resultado);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RespostaLoginDto>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { erro = "Refresh token é obrigatório." });

        var resultado = await servico.RefreshAsync(request.RefreshToken, cancellationToken);

        if (resultado is null)
            return Unauthorized(new { erro = "Token inválido ou expirado." });

        return Ok(resultado);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (long.TryParse(sub, out var usuarioId))
            await servico.RevogarTokenAsync(usuarioId, cancellationToken);

        return NoContent();
    }
}

public record LoginRequest(string Email, string Senha);
public record RefreshRequest(string RefreshToken);

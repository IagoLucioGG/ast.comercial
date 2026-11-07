using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AST.Comercial.Controllers;

/// <summary>
/// Visão pública (anônima) de propostas compartilhadas. A autorização é a posse da chave (aleatória, 256 bits).
/// Anônimo precisa de [AllowAnonymous] por causa do AuthorizeFilter global; o rate limit anônimo por IP continua valendo.
/// </summary>
[AllowAnonymous]
[Route("api/publico/propostas")]
public class PropostaPublicaController(IPropostaServico servico) : ControllerBase
{
    private ObjectResult Erro(int status, string mensagem) => StatusCode(status, new { erro = mensagem, status });

    private void ProtegerCache()
    {
        // A URL contém a chave: sem cache, sem indexação, sem vazar via Referer
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    [HttpGet("{chave}")]
    public async Task<ActionResult> Obter(string chave, CancellationToken ct)
    {
        ProtegerCache();
        var dto = await servico.ObterPublicaAsync(chave, ct);
        return dto is null ? Erro(404, "Proposta não encontrada.") : Ok(dto);
    }

    [HttpPost("{chave}/aceitar")]
    public async Task<ActionResult> Aceitar(string chave, [FromBody] AceitePropostaEntradaDto? corpo, CancellationToken ct)
    {
        ProtegerCache();
        if (corpo is null) return Erro(400, "Corpo da requisição inválido.");
        return Responder(await servico.AceitarPublicaAsync(chave, corpo.Nome, corpo.Email, ObterIp(), ct));
    }

    [HttpPost("{chave}/recusar")]
    public async Task<ActionResult> Recusar(string chave, [FromBody] RecusaPropostaEntradaDto? corpo, CancellationToken ct)
    {
        ProtegerCache();
        return Responder(await servico.RecusarPublicaAsync(chave, corpo?.Motivo, ObterIp(), ct));
    }

    private ActionResult Responder(RespostaPublicaResultado r) => r.Resultado switch
    {
        ResultadoRespostaPublica.Ok => Ok(new { status = r.Status }),
        ResultadoRespostaPublica.NaoEncontrada => Erro(404, "Proposta não encontrada."),
        ResultadoRespostaPublica.JaRespondida => Erro(409, r.Mensagem ?? "Esta proposta já foi respondida."),
        ResultadoRespostaPublica.NaoAceitaResposta => Erro(403, r.Mensagem ?? "Esta proposta não aceita resposta online."),
        _ => Erro(400, r.Mensagem ?? "Dados inválidos.")
    };

    /// <summary>IP de origem. X-Forwarded-For é informado entre parênteses porque pode ser forjado.</summary>
    private string ObterIp()
    {
        var remoto = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var xff = Request.Headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(xff)) return remoto;
        xff = xff.Length > 45 ? xff[..45] : xff;
        return $"{remoto} (X-Forwarded-For: {xff})";
    }
}

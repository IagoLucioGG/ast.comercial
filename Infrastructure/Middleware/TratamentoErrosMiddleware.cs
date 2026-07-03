using System.Text.Json;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Middleware global de tratamento de erros.
/// Nunca expõe stack trace ou detalhes internos em produção.
/// </summary>
public class TratamentoErrosMiddleware(RequestDelegate next, ILogger<TratamentoErrosMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Recurso não encontrado: {Path}", context.Request.Path);
            await EscreverResposta(context, 404, "Recurso não encontrado.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Acesso não autorizado: {Path}", context.Request.Path);
            await EscreverResposta(context, 403, "Acesso negado.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Operação inválida: {Path}", context.Request.Path);
            await EscreverResposta(context, 400, "Operação inválida.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno não tratado: {Path}", context.Request.Path);
            var mensagem = env.IsDevelopment() ? ex.Message : "Erro interno do servidor.";
            await EscreverResposta(context, 500, mensagem);
        }
    }

    private static async Task EscreverResposta(HttpContext context, int statusCode, string mensagem)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var resposta = new { erro = mensagem, status = statusCode };
        await context.Response.WriteAsync(JsonSerializer.Serialize(resposta));
    }
}

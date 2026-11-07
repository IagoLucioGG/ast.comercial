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
        catch (AST.Comercial.Application.RegraNegocioException ex)
        {
            logger.LogInformation("Regra de negócio violada: {Path} - {Mensagem}", context.Request.Path, ex.Message);
            await EscreverResposta(context, 422, ex.Message);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23503" or "23502" or "23505" })
        {
            var pg = (Npgsql.PostgresException)ex.InnerException!;
            logger.LogWarning(ex, "Violação de integridade ({Estado}): {Path}", pg.SqlState, context.Request.Path);
            switch (pg.SqlState)
            {
                case "23505":
                    await EscreverResposta(context, 409, "Já existe um registro com estes dados.");
                    break;
                case "23502":
                    await EscreverResposta(context, 422, "Há campo obrigatório não informado.");
                    break;
                default:
                    await EscreverResposta(context, 422, "Há referência a um registro que não existe (verifique os campos de vínculo, como cliente, funil ou etapa).");
                    break;
            }
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

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Rate limiting por usuário/API Key individual.
/// Garante isolamento total: o 429 de um usuário nunca afeta outro.
/// Compatível com nginx/EKS (usa X-Forwarded-For para IP real).
/// Limpeza automática de entradas expiradas para evitar memory leak.
/// </summary>
public class RateLimitMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<string, JanelaContador> Contadores = new();
    private static DateTime _ultimaLimpeza = DateTime.UtcNow;
    private static readonly object LockLimpeza = new();

    private const int LimiteUsuarioNormal = 600;
    private const int LimiteIntegracao = 1200;
    private const int LimiteAnonimo = 60;
    private const int JanelaSegundos = 60;
    private const int IntervaloLimpezaMinutos = 5;

    public async Task InvokeAsync(HttpContext context)
    {
        // Pular rate limit em ambiente de desenvolvimento
        if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            await next(context);
            return;
        }

        var (chave, limite) = ObterChaveELimite(context);
        var agora = DateTime.UtcNow;

        LimparExpiradosSeNecessario(agora);

        var contador = Contadores.GetOrAdd(chave, _ => new JanelaContador(agora));

        int quantidade;
        lock (contador)
        {
            if ((agora - contador.InicioJanela).TotalSeconds > JanelaSegundos)
            {
                contador.InicioJanela = agora;
                contador.Quantidade = 0;
            }

            contador.Quantidade++;
            quantidade = contador.Quantidade;
        }

        if (quantidade > limite)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = JanelaSegundos.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limite.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"erro\":\"Limite de requisições excedido. Tente novamente em breve.\",\"status\":429}");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limite.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limite - quantidade).ToString();

        await next(context);
    }

    private static (string Chave, int Limite) ObterChaveELimite(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
            return ($"key:{apiKey}", LimiteIntegracao);

        var sub = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub))
            return ($"user:{sub}", LimiteUsuarioNormal);

        var ip = ObterIpReal(context);
        return ($"ip:{ip}", LimiteAnonimo);
    }

    private static string ObterIpReal(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var primeiroIp = forwardedFor.ToString().Split(',', StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(primeiroIp))
                return primeiroIp;
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
            return realIp.ToString();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static void LimparExpiradosSeNecessario(DateTime agora)
    {
        if ((agora - _ultimaLimpeza).TotalMinutes < IntervaloLimpezaMinutos)
            return;

        lock (LockLimpeza)
        {
            if ((agora - _ultimaLimpeza).TotalMinutes < IntervaloLimpezaMinutos)
                return;

            _ultimaLimpeza = agora;

            var expirados = Contadores
                .Where(kv => (agora - kv.Value.InicioJanela).TotalSeconds > JanelaSegundos * 2)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var chave in expirados)
                Contadores.TryRemove(chave, out _);
        }
    }

    private class JanelaContador(DateTime inicio)
    {
        public DateTime InicioJanela { get; set; } = inicio;
        public int Quantidade { get; set; }
    }
}

using System.Text.RegularExpressions;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Middleware de sanitização de input.
/// Protege contra XSS, SQL Injection e payloads maliciosos.
/// JS/HTML é permitido em rotas de fórmula — segurança garantida pelo sandbox de execução.
/// </summary>
public partial class SanitizacaoMiddleware(RequestDelegate next, ILogger<SanitizacaoMiddleware> logger)
{
    private static readonly string[] RotasFormula = ["/odata/Campos", "/odata/RegrasCampo"];

    private static readonly string[] ApisProibidasGeral =
    [
        "__proto__", "constructor[", "prototype.",
        "<iframe", "<object", "<embed", "<form",
        "javascript:", "vbscript:", "data:text/html"
    ];

    private static readonly string[] ApisProibidasForaFormula =
    [
        "eval(", "Function(", "setTimeout(", "setInterval(",
        "document.", "window.", "globalThis.", "process.",
        "require(", "import(", "fetch(", "XMLHttpRequest"
    ];

    private static readonly string[] PadroesSql =
    [
        "'; DROP", "'; DELETE", "'; UPDATE", "'; INSERT",
        "UNION SELECT", "UNION ALL SELECT",
        "' OR '1'='1", "'; EXEC", "xp_cmdshell", "sp_executesql"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength is not > 0 ||
            context.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            await next(context);
            return;
        }

        var ehRotaFormula = RotasFormula.Any(r =>
            context.Request.Path.Value?.Contains(r, StringComparison.OrdinalIgnoreCase) == true);

        // 1. Prototype pollution e HTML injection — sempre bloqueados
        var detectado = Detectar(body, ApisProibidasGeral);
        if (detectado is not null)
        {
            Log(context, "Payload bloqueado", detectado);
            await Rejeitar(context);
            return;
        }

        // 2. APIs JS perigosas — bloqueadas fora de fórmulas
        if (!ehRotaFormula)
        {
            detectado = Detectar(body, ApisProibidasForaFormula);
            if (detectado is not null)
            {
                Log(context, "JS bloqueado fora de fórmula", detectado);
                await Rejeitar(context);
                return;
            }
        }

        // 3. SQL injection — sempre bloqueado
        if (DetectarSql(body))
        {
            Log(context, "SQL injection bloqueado", null);
            await Rejeitar(context);
            return;
        }

        // 4. Event handlers HTML — bloqueados fora de fórmulas
        if (!ehRotaFormula && EventHandlerRegex().IsMatch(body))
        {
            Log(context, "Event handler bloqueado", null);
            await Rejeitar(context);
            return;
        }

        // 5. Tags perigosas — bloqueadas fora de fórmulas
        if (!ehRotaFormula && ScriptTagRegex().IsMatch(body))
        {
            Log(context, "HTML tag bloqueada", null);
            await Rejeitar(context, "HTML e scripts não são permitidos neste campo.");
            return;
        }

        await next(context);
    }

    private static string? Detectar(string body, string[] padroes)
    {
        var lower = body.ToLowerInvariant();
        foreach (var padrao in padroes)
        {
            if (lower.Contains(padrao.ToLowerInvariant()))
                return padrao;
        }
        return null;
    }

    private static bool DetectarSql(string body)
    {
        var upper = body.ToUpperInvariant();
        return PadroesSql.Any(p => upper.Contains(p.ToUpperInvariant()));
    }

    private void Log(HttpContext context, string tipo, string? padrao)
    {
        logger.LogWarning("{Tipo}. IP: {Ip}, Rota: {Path}, Padrão: {Padrao}",
            tipo, ObterIp(context), context.Request.Path, padrao ?? "-");
    }

    private static async Task Rejeitar(HttpContext context, string mensagem = "Conteúdo não permitido detectado na requisição.")
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"erro\":\"{mensagem}\",\"status\":400}}");
    }

    private static string ObterIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
            return xff.ToString().Split(',').FirstOrDefault()?.Trim() ?? "unknown";
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    [GeneratedRegex(@"on[a-z]+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"<\s*script[\s>]|<\s*/\s*script\s*>|<\s*style[\s>]|<\s*link[\s>]|<\s*meta[\s>]", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();
}

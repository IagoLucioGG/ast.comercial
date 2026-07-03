using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Middleware que resolve o tenant (empresa) do usuário autenticado.
/// Suporta JWT Bearer (usuários normais) e API Key header (integração).
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // 1. Tentar via API Key (usuário de integração)
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
        {
            var usuario = await db.Usuarios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TokenIntegracao == apiKey.ToString() && u.Ativo && u.Tipo == TipoUsuario.Integracao);

            if (usuario is not null)
            {
                db.EmpresaIdAtual = usuario.EmpresaId;
                db.UsuarioAtual = usuario.Nome;
                await next(context);
                return;
            }
        }

        // 2. Via JWT Bearer (usuário normal)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var empresaIdClaim = context.User.FindFirst("empresa_id")?.Value;
            var nomeClaim = context.User.FindFirst("nome")?.Value;

            if (long.TryParse(empresaIdClaim, out var empresaId))
            {
                db.EmpresaIdAtual = empresaId;
                db.UsuarioAtual = nomeClaim ?? "Sistema";
            }
        }

        await next(context);
    }
}

using System.Security.Claims;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Resolve o tenant (empresa), o usuário, o perfil e o escopo de visibilidade da requisição.
/// Suporta JWT Bearer (usuários normais) e API Key header (integração).
///
/// Usuário de integração (X-Api-Key): é autenticado aqui (UseAuthentication não conhece a chave, então o
/// ClaimsPrincipal é montado neste ponto, antes do AuthorizeFilter do MVC) e passa a valer como
/// Administrador da própria empresa: acesso total e escopo Todos, como era antes da matriz de permissões.
///
/// Usuário normal: o perfil NÃO vem da claim do JWT (ficaria desatualizado até o token expirar) e sim do banco
/// (cache de 30 s); usuário inativo/removido recebe 401 mesmo com token ainda válido.
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, AppDbContext db, IPermissaoServico permissoes)
    {
        // 1. Tentar via API Key (usuário de integração)
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
        {
            var chave = apiKey.ToString();
            var usuario = await db.Usuarios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TokenIntegracao == chave && u.Ativo && u.Tipo == TipoUsuario.Integracao);

            if (usuario is not null)
            {
                db.EmpresaIdAtual = usuario.EmpresaId;
                db.UsuarioAtual = usuario.Nome;
                db.UsuarioIdAtual = usuario.Id;
                db.UsuarioNomeAtual = usuario.Nome;
                db.PerfilAtual = PerfilUsuario.Administrador;

                var identidade = new ClaimsIdentity(
                [
                    new Claim("sub", usuario.Id.ToString()),
                    new Claim("empresa_id", usuario.EmpresaId.ToString()),
                    new Claim("nome", usuario.Nome),
                    new Claim("perfil", nameof(PerfilUsuario.Administrador)),
                    new Claim("tipo", nameof(TipoUsuario.Integracao))
                ], "ApiKey");
                context.User = new ClaimsPrincipal(identidade);

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
                db.UsuarioNomeAtual = nomeClaim;
                var subClaim = context.User.FindFirst("sub")?.Value
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                db.UsuarioIdAtual = long.TryParse(subClaim, out var usuarioId) ? usuarioId : null;

                if (db.UsuarioIdAtual is { } id)
                {
                    var contexto = await permissoes.ObterContextoUsuarioAsync(empresaId, id, context.RequestAborted);
                    if (contexto is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { erro = "Usuário inativo ou inexistente.", status = 401 });
                        return;
                    }

                    db.PerfilAtual = contexto.Perfil;
                    await PreencherEscopoAsync(db, permissoes, contexto);
                }
            }
        }

        await next(context);
    }

    /// <summary>Define, no contexto da requisição, o escopo de Negócios e Atividades do perfil (Administrador = Todos).</summary>
    private static async Task PreencherEscopoAsync(AppDbContext db, IPermissaoServico permissoes, ContextoUsuario contexto)
    {
        if (contexto.Perfil == PerfilUsuario.Administrador) return;

        var matriz = await permissoes.ObterMatrizAsync(contexto.EmpresaId, contexto.Perfil);
        db.UsuariosDaEquipeAtual = contexto.CompanheirosDeEquipe;
        db.EscopoNegocio = matriz.TryGetValue(CatalogoEntidades.Negocio, out var n) ? n.Escopo : EscopoVisibilidade.Todos;
        db.EscopoAtividade = matriz.TryGetValue(CatalogoEntidades.Atividade, out var a) ? a.Escopo : EscopoVisibilidade.Todos;
    }
}

using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace AST.Comercial.Infrastructure.Autorizacao;

/// <summary>
/// Autorização global por perfil × entidade × ação para odata/* e api/*, sem editar controllers.
/// Roda depois do TenantMiddleware (que preenche perfil e escopo no AppDbContext).
/// Sem permissão: 403 { "erro": "Sem permissão para &lt;ação&gt; &lt;entidade&gt;.", "status": 403 }.
/// </summary>
public class AutorizacaoMiddleware(RequestDelegate next, ILogger<AutorizacaoMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db, IPermissaoServico permissoes)
    {
        var caminho = context.Request.Path;
        var controlado = caminho.StartsWithSegments("/odata", StringComparison.OrdinalIgnoreCase)
                         || caminho.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

        // Rotas anônimas ([AllowAnonymous]: login, refresh, proposta pública) e requisições sem usuário
        // (o AuthorizeFilter responde 401) não passam pela matriz.
        if (!controlado
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || db.PerfilAtual is null)
        {
            await next(context);
            return;
        }

        // Administrador (e usuário de integração, tratado como Administrador da empresa) sempre passa.
        if (db.PerfilAtual == PerfilUsuario.Administrador)
        {
            await next(context);
            return;
        }

        var regra = MapaPermissaoRotas.Resolver(context.Request.Method, caminho, context.Request.Query, db.UsuarioIdAtual);
        if (regra.Tipo == TipoRegra.Livre)
        {
            await next(context);
            return;
        }

        var matriz = await permissoes.ObterMatrizAsync(db.EmpresaIdAtual, db.PerfilAtual.Value, context.RequestAborted);

        if (Permitido(regra, matriz, out var entidadeNegada, out var acaoNegada))
        {
            await next(context);
            return;
        }

        string mensagem;
        if (regra.Tipo == TipoRegra.NaoMapeada || regra.Tipo == TipoRegra.SomenteAdministrador)
        {
            mensagem = "Sem permissão para acessar este recurso.";
            logger.LogWarning("Rota sem regra de permissão negada para não-administrador: {Metodo} {Caminho}",
                context.Request.Method, caminho.Value);
        }
        else
        {
            var rotulo = CatalogoEntidades.Todas.FirstOrDefault(e => e.Chave == entidadeNegada)?.Rotulo ?? entidadeNegada ?? "recurso";
            mensagem = $"Sem permissão para {NomeDaAcao(acaoNegada)} {rotulo}.";
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { erro = mensagem, status = 403 });
    }

    private static bool Permitido(
        RegraRota regra, IReadOnlyDictionary<string, PermissaoEfetiva> matriz,
        out string? entidadeNegada, out AcaoPermissao acaoNegada)
    {
        entidadeNegada = regra.Entidade;
        acaoNegada = regra.Acao;

        switch (regra.Tipo)
        {
            case TipoRegra.Entidade:
                if (regra.Acao == AcaoPermissao.Ver && regra.LeituraAlternativa is { Length: > 0 })
                    return Tem(matriz, regra.Entidade!, AcaoPermissao.Ver)
                           || regra.LeituraAlternativa.Any(e => Tem(matriz, e, AcaoPermissao.Ver));
                return Tem(matriz, regra.Entidade!, regra.Acao);

            case TipoRegra.EdicaoComercial:
                entidadeNegada = "registros";
                return MapaPermissaoRotas.EntidadesDeRegistro.Any(e =>
                    Tem(matriz, e, AcaoPermissao.Editar) || Tem(matriz, e, AcaoPermissao.Criar));

            case TipoRegra.VerComercialQualquer:
                entidadeNegada = "o histórico";
                return CatalogoEntidades.Todas
                    .Where(e => e.Grupo == CatalogoEntidades.GrupoComercial)
                    .Any(e => Tem(matriz, e.Chave, AcaoPermissao.Ver));

            default: // NaoMapeada e SomenteAdministrador (o administrador já saiu antes)
                return false;
        }
    }

    private static bool Tem(IReadOnlyDictionary<string, PermissaoEfetiva> matriz, string entidade, AcaoPermissao acao)
    {
        if (!matriz.TryGetValue(entidade, out var p)) return false;
        return acao switch
        {
            AcaoPermissao.Ver => p.Ver,
            AcaoPermissao.Criar => p.Criar && p.Ver,
            AcaoPermissao.Editar => p.Editar && p.Ver,
            AcaoPermissao.Excluir => p.Excluir && p.Ver,
            _ => false
        };
    }

    private static string NomeDaAcao(AcaoPermissao acao) => acao switch
    {
        AcaoPermissao.Ver => "ver",
        AcaoPermissao.Criar => "criar",
        AcaoPermissao.Editar => "editar",
        AcaoPermissao.Excluir => "excluir",
        _ => "acessar"
    };
}

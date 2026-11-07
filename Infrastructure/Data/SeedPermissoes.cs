using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Data;

/// <summary>
/// Matriz padrão de permissões por perfil × entidade. Idempotente: só insere pares (Perfil, Entidade) que faltam,
/// nunca altera o que o administrador já ajustou.
/// </summary>
public static class SeedPermissoes
{
    public static async Task SeedAsync(AppDbContext db, long empresaId)
    {
        var existentes = await db.PermissoesPerfil
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId)
            .Select(p => new { p.Perfil, p.Entidade })
            .ToListAsync();
        var jaTem = existentes.Select(e => (e.Perfil, e.Entidade)).ToHashSet();

        var novas = new List<PermissaoPerfil>();
        foreach (var perfil in Enum.GetValues<PerfilUsuario>())
        {
            foreach (var entidade in CatalogoEntidades.Todas)
            {
                if (jaTem.Contains((perfil, entidade.Chave))) continue;
                var (ver, criar, editar, excluir, escopo) = Padrao(perfil, entidade);
                novas.Add(new PermissaoPerfil
                {
                    EmpresaId = empresaId,
                    Perfil = perfil,
                    Entidade = entidade.Chave,
                    PodeVer = ver,
                    PodeCriar = criar,
                    PodeEditar = editar,
                    PodeExcluir = excluir,
                    Escopo = escopo
                });
            }
        }

        if (novas.Count == 0) return;
        db.PermissoesPerfil.AddRange(novas);
        await db.SaveChangesAsync();
    }

    private static (bool Ver, bool Criar, bool Editar, bool Excluir, EscopoVisibilidade Escopo) Padrao(
        PerfilUsuario perfil, EntidadePermissionavel e)
    {
        var chave = e.Chave;
        var comercial = e.Grupo == CatalogoEntidades.GrupoComercial;

        switch (perfil)
        {
            case PerfilUsuario.Administrador:
                return (true, true, true, true, EscopoVisibilidade.Todos);

            case PerfilUsuario.Gerente:
                if (comercial) return (true, true, true, true, EscopoVisibilidade.Todos);
                return (true, false, false, false, EscopoVisibilidade.Todos);

            case PerfilUsuario.Vendedor:
                switch (chave)
                {
                    case CatalogoEntidades.Cliente:
                    case CatalogoEntidades.PessoaContato:
                    case CatalogoEntidades.Proposta:
                        return (true, true, true, false, EscopoVisibilidade.Todos);
                    case CatalogoEntidades.Negocio:
                    case CatalogoEntidades.Atividade:
                        return (true, true, true, false, EscopoVisibilidade.Proprios);
                    case CatalogoEntidades.Produto:
                    case CatalogoEntidades.ListaPreco:
                    case CatalogoEntidades.Etiqueta:
                        return (true, false, false, false, EscopoVisibilidade.Todos);
                    case CatalogoEntidades.Meta:
                    case CatalogoEntidades.PainelRelatorio:
                        return (true, false, false, false, EscopoVisibilidade.Proprios);
                    default:
                        return (false, false, false, false, EscopoVisibilidade.Todos);
                }

            default: // Visualizador
                if (comercial && chave != CatalogoEntidades.Importacao)
                    return (true, false, false, false, EscopoVisibilidade.Todos);
                return (false, false, false, false, EscopoVisibilidade.Todos);
        }
    }
}

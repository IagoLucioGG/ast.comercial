using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AST.Comercial.Infrastructure.Autorizacao;

/// <summary>
/// Leitura da matriz de permissões e do contexto do usuário, com cache em memória.
/// A matriz fica 2 min em cache por (empresa, perfil) e é descartada ao salvar (PUT api/permissoes) nesta instância.
/// O contexto do usuário fica 30 s (perfil, ativo, equipes): troca de perfil, desativação ou mudança de equipe
/// passam a valer em até 30 s, sem precisar de novo login.
/// </summary>
public class PermissaoServico(AppDbContext db, IMemoryCache cache) : IPermissaoServico
{
    private static readonly TimeSpan TtlMatriz = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TtlUsuario = TimeSpan.FromSeconds(30);

    private sealed record Holder(ContextoUsuario? Valor);

    private static string ChaveMatriz(long empresaId, PerfilUsuario perfil) => $"perm:{empresaId}:{(int)perfil}";
    private static string ChaveUsuario(long empresaId, long usuarioId) => $"permusr:{empresaId}:{usuarioId}";

    public async Task<IReadOnlyDictionary<string, PermissaoEfetiva>> ObterMatrizAsync(
        long empresaId, PerfilUsuario perfil, CancellationToken ct = default)
    {
        var chave = ChaveMatriz(empresaId, perfil);
        if (cache.TryGetValue(chave, out IReadOnlyDictionary<string, PermissaoEfetiva>? existente) && existente is not null)
            return existente;

        var linhas = await db.PermissoesPerfil
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.EmpresaId == empresaId && p.Ativo && p.Perfil == perfil)
            .ToListAsync(ct);

        IReadOnlyDictionary<string, PermissaoEfetiva> matriz = linhas.ToDictionary(
            p => p.Entidade,
            p => new PermissaoEfetiva(p.PodeVer, p.PodeCriar, p.PodeEditar, p.PodeExcluir, p.Escopo));

        cache.Set(chave, matriz, TtlMatriz);
        return matriz;
    }

    public async Task<ContextoUsuario?> ObterContextoUsuarioAsync(long empresaId, long usuarioId, CancellationToken ct = default)
    {
        var chave = ChaveUsuario(empresaId, usuarioId);
        if (cache.TryGetValue(chave, out Holder? existente) && existente is not null)
            return existente.Valor;

        var usuario = await db.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == usuarioId && u.EmpresaId == empresaId && u.Ativo)
            .Select(u => new { u.Nome, u.Perfil, u.Tipo })
            .FirstOrDefaultAsync(ct);

        ContextoUsuario? contexto = null;
        if (usuario is not null)
        {
            var equipes = await db.MembrosEquipe
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && m.Ativo && m.UsuarioId == usuarioId && m.Equipe.Ativo)
                .Select(m => m.EquipeId)
                .ToListAsync(ct);

            var companheiros = equipes.Count == 0
                ? []
                : await db.MembrosEquipe
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(m => m.EmpresaId == empresaId && m.Ativo && equipes.Contains(m.EquipeId) && m.Usuario.Ativo)
                    .Select(m => m.UsuarioId)
                    .Distinct()
                    .ToListAsync(ct);

            contexto = new ContextoUsuario(usuarioId, empresaId, usuario.Nome, usuario.Perfil, usuario.Tipo, [.. companheiros]);
        }

        cache.Set(chave, new Holder(contexto), TtlUsuario);
        return contexto;
    }

    public void InvalidarEmpresa(long empresaId)
    {
        foreach (var perfil in Enum.GetValues<PerfilUsuario>())
            cache.Remove(ChaveMatriz(empresaId, perfil));
    }
}

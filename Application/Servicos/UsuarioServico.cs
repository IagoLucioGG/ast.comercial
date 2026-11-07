using System.Net.Mail;
using System.Security.Cryptography;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class UsuarioServico(AppDbContext db) : IUsuarioServico
{
    private const int SenhaMinima = 8;
    private const int SenhaMaxima = 100;

    // SenhaHash e TokenIntegracao são IgnoreDataMember na entidade: não saem na API.
    public IQueryable<Usuario> ObterTodos()
        => db.Usuarios.AsNoTracking();

    // Inclui inativos (a tela de administração precisa reativar). Sempre restrito à empresa atual.
    public IQueryable<Usuario> ObterTodosInclusiveInativos()
        => db.Usuarios.IgnoreQueryFilters().AsNoTracking().Where(u => u.EmpresaId == db.EmpresaIdAtual);

    public async Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Usuario> CriarAsync(NovoUsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = new Usuario
        {
            EmpresaId = db.EmpresaIdAtual,
            Nome = (request.Nome ?? "").Trim(),
            Email = (request.Email ?? "").Trim().ToLowerInvariant(),
            Perfil = ParseEnum(request.Perfil, PerfilUsuario.Vendedor, "Perfil"),
            Tipo = ParseEnum(request.Tipo, TipoUsuario.Normal, "Tipo"),
            CargoId = request.CargoId,
            DepartamentoId = request.DepartamentoId,
            FotoUrl = LimparFoto(request.FotoUrl),
            DescricaoIntegracao = string.IsNullOrWhiteSpace(request.DescricaoIntegracao) ? null : request.DescricaoIntegracao.Trim(),
        };
        if (usuario.DescricaoIntegracao is { Length: > 300 })
            throw new RegraNegocioException("A descrição da integração deve ter no máximo 300 caracteres.");
        await ValidarAsync(usuario, ct);

        if (usuario.Tipo == TipoUsuario.Normal)
        {
            ValidarSenha(request.Senha);
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);
        }
        else
        {
            usuario.TokenIntegracao = NovoToken();
        }

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return usuario;
    }

    public async Task<Usuario?> AtualizarAsync(long id, Delta<Usuario> delta, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == db.EmpresaIdAtual, ct);
        if (usuario is null) return null;

        var eraAdminAtivo = usuario.Ativo && usuario.Perfil == PerfilUsuario.Administrador && usuario.Tipo == TipoUsuario.Normal;
        var (empresaId, criadoEm, tipo, ultimoAcesso, chave) =
            (usuario.EmpresaId, usuario.CriadoEm, usuario.Tipo, usuario.UltimoAcesso, usuario.ChaveExterna);

        delta.Patch(usuario);

        // Campos que o cliente não pode alterar por PATCH.
        usuario.Id = id;
        usuario.EmpresaId = empresaId;
        usuario.CriadoEm = criadoEm;
        usuario.Tipo = tipo;
        usuario.UltimoAcesso = ultimoAcesso;
        usuario.ChaveExterna = chave;
        usuario.Nome = (usuario.Nome ?? "").Trim();
        usuario.Email = (usuario.Email ?? "").Trim().ToLowerInvariant();
        usuario.FotoUrl = LimparFoto(usuario.FotoUrl);
        usuario.DescricaoIntegracao = string.IsNullOrWhiteSpace(usuario.DescricaoIntegracao) ? null : usuario.DescricaoIntegracao.Trim();
        if (usuario.DescricaoIntegracao is { Length: > 300 })
            throw new RegraNegocioException("A descrição da integração deve ter no máximo 300 caracteres.");
        await ValidarAsync(usuario, ct);

        var segueAdminAtivo = usuario.Ativo && usuario.Perfil == PerfilUsuario.Administrador;
        if (eraAdminAtivo && !segueAdminAtivo)
            await ExigirOutroAdminAsync(id, ct);

        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return usuario;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null) return false;
        if (usuario.Perfil == PerfilUsuario.Administrador && usuario.Tipo == TipoUsuario.Normal)
            await ExigirOutroAdminAsync(id, ct);
        usuario.Ativo = false;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await RevogarTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> RegenerarTokenIntegracaoAsync(long id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        if (usuario.Tipo != TipoUsuario.Integracao)
            throw new InvalidOperationException("Apenas usuários de integração podem ter token regenerado.");
        usuario.TokenIntegracao = NovoToken();
        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return usuario.TokenIntegracao;
    }

    public async Task AlterarSenhaAsync(long id, string senhaAtual, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        if (usuario.Tipo != TipoUsuario.Normal)
            throw new InvalidOperationException("Usuários de integração não possuem senha.");
        if (!string.IsNullOrEmpty(usuario.SenhaHash) && !BCrypt.Net.BCrypt.Verify(senhaAtual, usuario.SenhaHash))
            throw new UnauthorizedAccessException("Senha atual incorreta.");
        ValidarSenha(novaSenha);
        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RedefinirSenhaAsync(long id, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        if (usuario.Tipo != TipoUsuario.Normal)
            throw new InvalidOperationException("Usuários de integração não possuem senha.");
        ValidarSenha(novaSenha);
        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
        usuario.AtualizadoEm = DateTime.UtcNow;
        await RevogarTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- helpers

    private async Task RevogarTokensAsync(long usuarioId, CancellationToken ct)
    {
        var tokens = await db.TokensAcesso.Where(t => t.UsuarioId == usuarioId && !t.Revogado).ToListAsync(ct);
        foreach (var t in tokens) { t.Revogado = true; t.RevogadoEm = DateTime.UtcNow; }
    }

    private async Task ExigirOutroAdminAsync(long id, CancellationToken ct)
    {
        var outro = await db.Usuarios.AnyAsync(u => u.Id != id && u.Perfil == PerfilUsuario.Administrador && u.Tipo == TipoUsuario.Normal, ct);
        if (!outro)
            throw new RegraNegocioException("A empresa precisa ter ao menos um administrador ativo.");
    }

    private async Task ValidarAsync(Usuario u, CancellationToken ct)
    {
        if (u.Nome.Length == 0) throw new RegraNegocioException("O nome é obrigatório.");
        if (u.Nome.Length > 150) throw new RegraNegocioException("O nome deve ter no máximo 150 caracteres.");
        if (!EmailValido(u.Email)) throw new RegraNegocioException("E-mail inválido.");
        if (!Enum.IsDefined(u.Perfil)) throw new RegraNegocioException("Perfil inválido.");
        if (!Enum.IsDefined(u.Tipo)) throw new RegraNegocioException("Tipo inválido.");

        // O índice de e-mail é global (todas as empresas, inclusive inativos).
        var email = u.Email;
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Id != u.Id && x.Email.ToLower() == email, ct))
            throw new RegraNegocioException("Já existe um usuário com este e-mail.");

        if (u.CargoId is long cargoId && !await db.Cargos.AnyAsync(c => c.Id == cargoId, ct))
            throw new RegraNegocioException("Cargo não encontrado.");
        if (u.DepartamentoId is long depId && !await db.Departamentos.AnyAsync(d => d.Id == depId, ct))
            throw new RegraNegocioException("Departamento não encontrado.");
    }

    private static void ValidarSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < SenhaMinima)
            throw new RegraNegocioException($"A senha deve ter ao menos {SenhaMinima} caracteres.");
        if (senha.Length > SenhaMaxima)
            throw new RegraNegocioException($"A senha deve ter no máximo {SenhaMaxima} caracteres.");
    }

    private static bool EmailValido(string email)
    {
        if (email.Length == 0 || email.Length > 150) return false;
        try { return new MailAddress(email).Address == email; } catch { return false; }
    }

    private static string? LimparFoto(string? url)
    {
        var v = url?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Length > 500) throw new RegraNegocioException("A URL da foto deve ter no máximo 500 caracteres.");
        if (!(v.StartsWith("https://") || v.StartsWith("http://") || (v.StartsWith('/') && !v.StartsWith("//"))))
            throw new RegraNegocioException("A URL da foto deve começar com http://, https:// ou /.");
        return v;
    }

    private static TEnum ParseEnum<TEnum>(string? valor, TEnum padrao, string campo) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor)) return padrao;
        if (Enum.TryParse<TEnum>(valor.Trim(), true, out var r) && Enum.IsDefined(r)) return r;
        throw new RegraNegocioException($"{campo} inválido.");
    }

    private static string NovoToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

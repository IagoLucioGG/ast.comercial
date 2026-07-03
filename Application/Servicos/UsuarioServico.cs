using System.Security.Cryptography;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class UsuarioServico(AppDbContext db) : IUsuarioServico
{
    public IQueryable<Usuario> ObterTodos()
        => db.Usuarios.AsNoTracking();

    public async Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Usuario> CriarAsync(Delta<Usuario> delta, CancellationToken ct = default)
    {
        var usuario = new Usuario { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(usuario);

        if (!string.IsNullOrEmpty(usuario.SenhaHash) && !usuario.SenhaHash.StartsWith("$2"))
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.SenhaHash);

        if (string.IsNullOrEmpty(usuario.SenhaHash) && usuario.Tipo == TipoUsuario.Normal)
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword("Temp@123");

        if (usuario.Tipo == TipoUsuario.Integracao && string.IsNullOrEmpty(usuario.TokenIntegracao))
            usuario.TokenIntegracao = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return usuario;
    }

    public async Task<Usuario?> AtualizarAsync(long id, Delta<Usuario> delta, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null) return null;

        delta.Patch(usuario);
        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return usuario;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null) return false;

        usuario.Ativo = false;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> RegenerarTokenIntegracaoAsync(long id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");

        if (usuario.Tipo != TipoUsuario.Integracao)
            throw new InvalidOperationException("Apenas usuários de integração podem ter token regenerado.");

        usuario.TokenIntegracao = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
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

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
        await db.SaveChangesAsync(ct);
    }
}

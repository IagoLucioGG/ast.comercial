using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AST.Comercial.Application.Servicos;

public class AutenticacaoServico(AppDbContext db, IConfiguration config) : IAutenticacaoServico
{
    private const int TokenExpiracaoMinutos = 60;
    private const int RefreshExpiracaoDias = 7;

    public async Task<RespostaLoginDto?> LoginAsync(string email, string senha, string? origem = null, CancellationToken cancellationToken = default)
    {
        var usuario = await db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.Ativo && u.Tipo == TipoUsuario.Normal, cancellationToken);

        if (usuario is null || !VerificarSenha(senha, usuario.SenhaHash!))
            return null;

        usuario.UltimoAcesso = DateTime.UtcNow;
        var resposta = GerarTokens(usuario, origem);
        await db.SaveChangesAsync(cancellationToken);

        return resposta;
    }

    public async Task<RespostaLoginDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenExistente = await db.TokensAcesso
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.Revogado, cancellationToken);

        if (tokenExistente is null || tokenExistente.RefreshExpiraEm < DateTime.UtcNow)
            return null;

        tokenExistente.Revogado = true;
        tokenExistente.RevogadoEm = DateTime.UtcNow;

        var resposta = GerarTokens(tokenExistente.Usuario, tokenExistente.Origem);
        await db.SaveChangesAsync(cancellationToken);

        return resposta;
    }

    public async Task RevogarTokenAsync(long usuarioId, CancellationToken cancellationToken = default)
    {
        await db.TokensAcesso
            .Where(t => t.UsuarioId == usuarioId && !t.Revogado)
            .ExecuteUpdateAsync(t => t
                .SetProperty(x => x.Revogado, true)
                .SetProperty(x => x.RevogadoEm, DateTime.UtcNow), cancellationToken);
    }

    private RespostaLoginDto GerarTokens(Usuario usuario, string? origem)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(TokenExpiracaoMinutos);
        var jwt = GerarJwt(usuario, expiraEm);
        var refresh = GerarRefreshToken();

        db.TokensAcesso.Add(new TokenAcesso
        {
            UsuarioId = usuario.Id,
            Token = jwt,
            RefreshToken = refresh,
            ExpiraEm = expiraEm,
            RefreshExpiraEm = DateTime.UtcNow.AddDays(RefreshExpiracaoDias),
            Origem = origem
        });

        return new RespostaLoginDto
        {
            Token = jwt,
            RefreshToken = refresh,
            ExpiraEm = expiraEm,
            UsuarioId = usuario.Id,
            EmpresaId = usuario.EmpresaId,
            Nome = usuario.Nome,
            Perfil = usuario.Perfil.ToString()
        };
    }

    private string GerarJwt(Usuario usuario, DateTime expiraEm)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Chave"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", usuario.Id.ToString()),
            new Claim("empresa_id", usuario.EmpresaId.ToString()),
            new Claim("nome", usuario.Nome),
            new Claim("email", usuario.Email),
            new Claim("perfil", usuario.Perfil.ToString()),
            new Claim("tipo", usuario.Tipo.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Emissor"],
            audience: config["Jwt:Audiencia"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GerarRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static bool VerificarSenha(string senha, string hash)
        => BCrypt.Net.BCrypt.Verify(senha, hash);
}

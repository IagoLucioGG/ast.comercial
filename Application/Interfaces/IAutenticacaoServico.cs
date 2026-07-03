namespace AST.Comercial.Application.Interfaces;

public interface IAutenticacaoServico
{
    Task<RespostaLoginDto?> LoginAsync(string email, string senha, string? origem = null, CancellationToken cancellationToken = default);
    Task<RespostaLoginDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevogarTokenAsync(long usuarioId, CancellationToken cancellationToken = default);
}

public record RespostaLoginDto
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiraEm { get; init; }
    public long UsuarioId { get; init; }
    public long EmpresaId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
}

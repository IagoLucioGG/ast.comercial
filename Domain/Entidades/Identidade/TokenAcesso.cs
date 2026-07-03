namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Token JWT emitido no login de usuários normais.
/// Cada login gera um novo token com expiração.
/// </summary>
public class TokenAcesso
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime EmitidoEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; }
    public DateTime? RefreshExpiraEm { get; set; }
    public bool Revogado { get; set; }
    public DateTime? RevogadoEm { get; set; }

    /// <summary>
    /// IP/User-Agent de quem gerou o token (auditoria)
    /// </summary>
    public string? Origem { get; set; }

    public bool EstaValido => !Revogado && ExpiraEm > DateTime.UtcNow;
}


namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Usuário do sistema - vinculado a uma empresa (tenant).
/// Pode ser um usuário normal (login com senha) ou de integração (token fixo).
/// </summary>
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SenhaHash { get; set; }
    public TipoUsuario Tipo { get; set; } = TipoUsuario.Normal;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Vendedor;
    public bool EmailConfirmado { get; set; }
    public DateTime? UltimoAcesso { get; set; }

    /// <summary>
    /// Token fixo para usuários de integração (API Key).
    /// Gerado uma vez, não expira (pode ser revogado desativando o usuário).
    /// </summary>
    public string? TokenIntegracao { get; set; }

    /// <summary>
    /// URL da foto do perfil do usuário.
    /// </summary>
    public string? FotoUrl { get; set; }

    /// <summary>
    /// Descrição do propósito da integração (ex: "Integração ERP", "Webhook Bimer")
    /// </summary>
    public string? DescricaoIntegracao { get; set; }

    public long? CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    public long? DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }

    public Empresa Empresa { get; set; } = null!;

    public ICollection<TokenAcesso> Tokens { get; set; } = [];
    public ICollection<MembroEquipe> Equipes { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

public enum TipoUsuario
{
    Normal = 0,
    Integracao = 1
}

public enum PerfilUsuario
{
    Administrador = 0,
    Gerente = 1,
    Vendedor = 2,
    Visualizador = 3
}


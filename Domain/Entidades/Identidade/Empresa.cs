namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Empresa/Tenant - define o escopo de dados de cada organização
/// </summary>
public class Empresa : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public string? LogoUrl { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}


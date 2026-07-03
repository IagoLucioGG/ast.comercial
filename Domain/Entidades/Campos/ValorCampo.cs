namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Valor de uma propriedade personalizada (OtherProperties).
/// Modelo idêntico ao da Ploomes: colunas tipadas + referências a outras entidades.
/// Acessado via $expand=OutrasPropriedades
/// </summary>
public class ValorCampo : EntidadeBase
{
    public long CampoId { get; set; }
    public Campo Campo { get; set; } = null!;

    public string CampoChave { get; set; } = string.Empty;

    // FK para a entidade dona (Cliente, Negocio, etc.)
    public long RegistroId { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }

    // Valores tipados
    public string? ValorTexto { get; set; }
    public string? ValorTextoGrande { get; set; }
    public int? ValorInteiro { get; set; }
    public decimal? ValorDecimal { get; set; }
    public DateTime? ValorDataHora { get; set; }
    public bool? ValorBooleano { get; set; }

    // Referência a opção de lista (ObjectValue na Ploomes)
    public long? OpcaoValorId { get; set; }
    public string? OpcaoValorNome { get; set; }

    // Referência a usuário
    public long? UsuarioValorId { get; set; }
    public string? UsuarioValorNome { get; set; }
    public string? UsuarioValorAvatarUrl { get; set; }

    // Referência a produto
    public long? ProdutoValorId { get; set; }
    public string? ProdutoValorNome { get; set; }

    // Referência a cliente
    public long? ClienteValorId { get; set; }
    public string? ClienteValorNome { get; set; }
    public string? ClienteValorDocumento { get; set; }

    // Referência a moeda
    public long? MoedaValorId { get; set; }

    // Anexo
    public long? AnexoValorId { get; set; }
    public string? AnexoValorNome { get; set; }
}


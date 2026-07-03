namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Proposta/Cotação (equivalente ao Quote da Ploomes).
/// Pode ser um template (IsTemplate=true) ou uma proposta real vinculada a um negócio.
/// </summary>
public class Proposta : EntidadeBase
{
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public string? Observacoes { get; set; }
    public int? Numero { get; set; }
    public int? Revisao { get; set; }
    public bool UltimaRevisao { get; set; } = true;
    public long? UltimaRevisaoId { get; set; }
    public bool IsTemplate { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public DateTime? DataExpiracao { get; set; }

    // Valores
    public decimal? Valor { get; set; }
    public decimal? Desconto { get; set; }
    public decimal? CustoFrete { get; set; }
    public string? ModalFrete { get; set; }
    public int? NumeroParcelas { get; set; }
    public string? PrazoEntrega { get; set; }
    public string? FormaPagamento { get; set; }
    public long? MoedaId { get; set; }

    // Aprovação
    public StatusAprovacaoProposta? StatusAprovacao { get; set; }
    public long? AprovadorId { get; set; }

    // Documento gerado
    public string? CodigoFonteHeader { get; set; }
    public int? AlturaHeader { get; set; }
    public string? CodigoFonteFooter { get; set; }
    public int? AlturaFooter { get; set; }
    public string? CodigoFonteBody { get; set; }
    public string? CodigoFontePreview { get; set; }
    public string? CodigoFonteCapa { get; set; }
    public bool TemCapa { get; set; }
    public bool TemPaginacao { get; set; }
    public int? MargemSuperior { get; set; }
    public int? MargemInferior { get; set; }
    public int? MargemLateral { get; set; }
    public string? NomeArquivo { get; set; }
    public string? UrlDocumento { get; set; }

    // Compartilhamento
    public string? Chave { get; set; }
    public bool Compartilhada { get; set; }
    public DateTime? DataCompartilhamento { get; set; }
    public bool AceitaExternamente { get; set; }
    public bool NotificacoesExternas { get; set; }
    public DateTime? UltimaAberturaExterna { get; set; }

    // Relacionamentos
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public long? NegocioId { get; set; }
    public Negocio? Negocio { get; set; }

    public long? ResponsavelId { get; set; }
    public long? TemplateId { get; set; }
    public long? CriadorId { get; set; }
    public long? AtualizadorId { get; set; }

    public ICollection<ItemProposta> Produtos { get; set; } = [];
    public ICollection<SecaoProposta> Secoes { get; set; } = [];
    public ICollection<ParcelaProposta> Parcelas { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

public enum StatusAprovacaoProposta
{
    Pendente = 0,
    Aprovada = 1,
    Rejeitada = 2,
    EmRevisao = 3
}

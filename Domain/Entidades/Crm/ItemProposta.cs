namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Produto dentro de uma proposta (equivalente ao QuoteProduct da Ploomes)
/// </summary>
public class ItemProposta : EntidadeBase
{
    public long PropostaId { get; set; }
    public Proposta Proposta { get; set; } = null!;

    public long? SecaoId { get; set; }
    public SecaoProposta? Secao { get; set; }

    public long? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public string? ProdutoNome { get; set; }
    public string? ProdutoCodigo { get; set; }
    public double? Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; }
    public double? Desconto { get; set; }
    public decimal? Total { get; set; }
    public int Ordem { get; set; }
    public long? MoedaId { get; set; }

    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Seção dentro de uma proposta (equivalente ao QuoteSection da Ploomes)
/// </summary>
public class SecaoProposta : EntidadeBase
{
    public string? Nome { get; set; }
    public int Ordem { get; set; }
    public decimal? SubTotal { get; set; }

    public long PropostaId { get; set; }
    public Proposta Proposta { get; set; } = null!;

    public ICollection<ItemProposta> Produtos { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

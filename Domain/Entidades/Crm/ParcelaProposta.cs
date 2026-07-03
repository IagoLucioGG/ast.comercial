namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Parcela/Installment de uma proposta (equivalente ao QuoteInstallment da Ploomes)
/// </summary>
public class ParcelaProposta : EntidadeBase
{
    public int Numero { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal? Valor { get; set; }
    public decimal? Percentual { get; set; }
    public string? Descricao { get; set; }

    public long PropostaId { get; set; }
    public Proposta Proposta { get; set; } = null!;

    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

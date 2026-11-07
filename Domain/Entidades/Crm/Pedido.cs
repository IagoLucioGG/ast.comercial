namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Pedido/Venda (equivalente ao Order da Ploomes). Nasce de uma proposta aceita/aprovada,
/// com os itens copiados no momento da geração (a proposta continua intacta).
/// </summary>
public class Pedido : EntidadeBase
{
    public int Numero { get; set; }
    public string? Titulo { get; set; }
    public string? Observacoes { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;

    // Valores (copiados da proposta de origem)
    public decimal? Valor { get; set; }
    public decimal? Desconto { get; set; }
    public decimal? CustoFrete { get; set; }
    public int? NumeroParcelas { get; set; }
    public string? FormaPagamento { get; set; }
    public long? MoedaId { get; set; }

    // Relacionamentos
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public long? NegocioId { get; set; }
    public Negocio? Negocio { get; set; }

    public long? PropostaId { get; set; }
    public Proposta? Proposta { get; set; }

    public long? EtapaPedidoId { get; set; }
    public EtapaPedido? Etapa { get; set; }

    public long? ResponsavelId { get; set; }

    public ICollection<ItemPedido> Itens { get; set; } = [];
}

/// <summary>Produto dentro do pedido (equivalente ao OrderProduct da Ploomes).</summary>
public class ItemPedido : EntidadeBase
{
    public long PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

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
}

/// <summary>Etapa do processo de venda (equivalente ao Orders@Stages da Ploomes).</summary>
public class EtapaPedido : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public string? Cor { get; set; }
}

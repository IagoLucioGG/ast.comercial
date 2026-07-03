namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Produto/Serviço que pode ser vinculado a negócios
/// </summary>
public class Produto : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Codigo { get; set; }
    public decimal Preco { get; set; }
    public decimal? PrecoCusto { get; set; }
    public string? Unidade { get; set; }
    public CategoriaProduto Categoria { get; set; }
    public bool Disponivel { get; set; } = true;

    public long? FamiliaProdutoId { get; set; }
    public FamiliaProduto? FamiliaProduto { get; set; }

    public long? GrupoProdutoId { get; set; }
    public GrupoProduto? GrupoProduto { get; set; }

    public long? MoedaId { get; set; }

    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

public enum CategoriaProduto
{
    Produto = 0,
    Servico = 1,
    Assinatura = 2
}





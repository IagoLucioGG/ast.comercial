namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Grupo de produto (subdivisão dentro da família)
/// </summary>
public class GrupoProduto : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public long? FamiliaProdutoId { get; set; }
    public FamiliaProduto? FamiliaProduto { get; set; }
    public int Ordem { get; set; }
}

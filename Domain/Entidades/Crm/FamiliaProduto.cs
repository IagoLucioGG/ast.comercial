namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Família/Categoria de produto (ex: Software, Hardware, Serviço)
/// </summary>
public class FamiliaProduto : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public ICollection<GrupoProduto> Grupos { get; set; } = [];
}

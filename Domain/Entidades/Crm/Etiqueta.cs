namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Etiqueta/Tag para classificação livre
/// </summary>
public class Etiqueta : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
}

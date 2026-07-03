namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Moeda (BRL, USD, EUR, etc.)
/// </summary>
public class Moeda : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? Simbolo { get; set; }
    public bool Padrao { get; set; }
}

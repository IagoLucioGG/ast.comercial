namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Origem/Fonte do contato (site, indicação, evento, etc.)
/// </summary>
public class OrigemContato : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }
}

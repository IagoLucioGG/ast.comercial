namespace AST.Comercial.Domain.Entidades;

public class Pais
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoTelefone { get; set; }

    public ICollection<Estado> Estados { get; set; } = [];
}

namespace AST.Comercial.Domain.Entidades;

public class Cidade
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoIbge { get; set; }

    public long EstadoId { get; set; }
    public Estado Estado { get; set; } = null!;
}

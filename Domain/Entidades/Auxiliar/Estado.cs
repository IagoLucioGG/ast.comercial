namespace AST.Comercial.Domain.Entidades;

public class Estado
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;

    public long PaisId { get; set; }
    public Pais Pais { get; set; } = null!;

    public ICollection<Cidade> Cidades { get; set; } = [];
}

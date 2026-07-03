namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Localidade hierárquica: País → Estado → Cidade.
/// Tabela global (não usa multi-tenant).
/// </summary>
public class Localidade
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoLocalidade Tipo { get; set; }
    public string? Codigo { get; set; }

    public long? LocalidadePaiId { get; set; }
    public Localidade? LocalidadePai { get; set; }
    public ICollection<Localidade> Filhos { get; set; } = [];
}

public enum TipoLocalidade
{
    Pais = 0,
    Estado = 1,
    Cidade = 2
}

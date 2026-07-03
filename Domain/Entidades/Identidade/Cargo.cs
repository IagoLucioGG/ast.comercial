namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Cargo/Role na empresa (equivalente ao Role da Ploomes)
/// </summary>
public class Cargo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Nivel { get; set; }

    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}


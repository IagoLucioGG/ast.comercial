namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Departamento da empresa (equivalente ao Department da Ploomes)
/// </summary>
public class Departamento : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public long? DepartamentoPaiId { get; set; }
    public Departamento? DepartamentoPai { get; set; }

    public ICollection<Departamento> SubDepartamentos { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}


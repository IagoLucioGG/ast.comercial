namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Faixa de número de funcionários / porte da empresa
/// </summary>
public class PorteEmpresa : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

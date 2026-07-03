namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Status configurável do cliente (Prospect, Ativo, Inativo, etc.)
/// Totalmente personalizável por empresa.
/// </summary>
public class StatusCliente : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public int Ordem { get; set; }
    public bool Padrao { get; set; }
}

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Motivo de perda de um negócio (preço, concorrência, timing, etc.)
/// Configurável pelo admin.
/// </summary>
public class MotivoPerda : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

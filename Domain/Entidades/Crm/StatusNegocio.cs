namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Status configurável do negócio (Aberto, Ganho, Perdido, ou qualquer outro).
/// O admin define quais status existem e qual é o comportamento de cada um.
/// </summary>
public class StatusNegocio : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public int Ordem { get; set; }
    public bool Padrao { get; set; }

    /// <summary>
    /// Tipo base do status — indica o comportamento no funil
    /// </summary>
    public TipoStatusNegocio TipoBase { get; set; }
}

/// <summary>
/// Comportamento base do status (o admin pode renomear, mas o sistema sabe o que cada tipo faz)
/// </summary>
public enum TipoStatusNegocio
{
    Aberto = 0,
    Ganho = 1,
    Perdido = 2
}

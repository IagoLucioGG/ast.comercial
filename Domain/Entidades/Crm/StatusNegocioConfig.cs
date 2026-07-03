namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Status configurável do negócio.
/// O admin define quais status existem e como se comportam por funil.
/// </summary>
public class StatusNegocioConfig : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public int Ordem { get; set; }
    public bool Padrao { get; set; }
    public ComportamentoStatus Comportamento { get; set; }

    public long? FunilId { get; set; }
    public Funil? Funil { get; set; }
}

public enum ComportamentoStatus
{
    Aberto = 0,
    Ganho = 1,
    Perdido = 2,
    Congelado = 3
}

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Opção disponível para campos do tipo Lista ou MultiLista
/// </summary>
public class OpcaoCampo : EntidadeBase
{
    public string Valor { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public string? Cor { get; set; }

    public long CampoId { get; set; }
    public Campo Campo { get; set; } = null!;
}





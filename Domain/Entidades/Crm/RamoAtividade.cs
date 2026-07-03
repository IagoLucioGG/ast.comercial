namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Ramo de atividade / Setor de atuação do contato
/// </summary>
public class RamoAtividade : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

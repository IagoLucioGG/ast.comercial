namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Vinculação N:N entre Etiqueta e qualquer registro
/// </summary>
public class EtiquetaRegistro : EntidadeBase
{
    public long EtiquetaId { get; set; }
    public Etiqueta Etiqueta { get; set; } = null!;

    public long RegistroId { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }
}

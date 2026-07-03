namespace AST.Comercial.Domain.Entidades;

public abstract class EntidadeBase
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Identificador no sistema externo (ex: ID do Bimer).
    /// Se preenchido, indica que o registro já foi sincronizado.
    /// </summary>
    public string? ChaveExterna { get; set; }
}





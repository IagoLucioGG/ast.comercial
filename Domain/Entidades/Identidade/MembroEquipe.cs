namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Vinculação entre usuário e equipe (N:N com dados extras)
/// </summary>
public class MembroEquipe : EntidadeBase
{
    public long EquipeId { get; set; }
    public Equipe Equipe { get; set; } = null!;

    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public PapelEquipe Papel { get; set; } = PapelEquipe.Membro;
}

public enum PapelEquipe
{
    Lider = 0,
    Membro = 1
}


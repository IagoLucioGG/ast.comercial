namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Equipe de vendas/atendimento (equivalente ao Team da Ploomes)
/// </summary>
public class Equipe : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public ICollection<MembroEquipe> Membros { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}


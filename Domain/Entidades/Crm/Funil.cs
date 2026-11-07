namespace AST.Comercial.Domain.Entidades;

public class Funil : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }

    /// <summary>Se verdadeiro, um negócio não pode voltar para etapa de ordem menor.</summary>
    public bool ProibirRetornoEtapa { get; set; }

    /// <summary>Se verdadeiro, um negócio só avança para a etapa imediatamente seguinte (sem pular).</summary>
    public bool ExigirTodasEtapas { get; set; }

    public ICollection<EtapaFunil> Etapas { get; set; } = [];
    public ICollection<Negocio> Negocios { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}





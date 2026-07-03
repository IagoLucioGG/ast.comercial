namespace AST.Comercial.Domain.Entidades;

public class Funil : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }

    public ICollection<EtapaFunil> Etapas { get; set; } = [];
    public ICollection<Negocio> Negocios { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}





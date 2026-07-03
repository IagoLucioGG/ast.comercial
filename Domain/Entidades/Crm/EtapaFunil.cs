namespace AST.Comercial.Domain.Entidades;

public class EtapaFunil : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int? DiasParaExpirar { get; set; }

    public long FunilId { get; set; }
    public Funil Funil { get; set; } = null!;

    public ICollection<Negocio> Negocios { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}





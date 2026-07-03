namespace AST.Comercial.Domain.Entidades;

public class Negocio : EntidadeBase
{
    public string Titulo { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public DateTime? FechadoEm { get; set; }
    public DateTime? PrevisaoFechamento { get; set; }

    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public long? PessoaContatoId { get; set; }
    public PessoaContato? PessoaContato { get; set; }

    public long FunilId { get; set; }
    public Funil Funil { get; set; } = null!;

    public long EtapaId { get; set; }
    public EtapaFunil Etapa { get; set; } = null!;

    public long? StatusId { get; set; }
    public StatusNegocio? Status { get; set; }

    public long? MotivoPerdaId { get; set; }
    public MotivoPerda? MotivoPerda { get; set; }

    public long? MoedaId { get; set; }

    public ICollection<Atividade> Atividades { get; set; } = [];
    public ICollection<Proposta> Propostas { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

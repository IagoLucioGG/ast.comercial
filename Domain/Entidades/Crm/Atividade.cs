namespace AST.Comercial.Domain.Entidades;

public class Atividade : EntidadeBase
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public TipoAtividade Tipo { get; set; }
    public DateTime? DataVencimento { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public bool Concluida { get; set; }

    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public long? PessoaContatoId { get; set; }
    public PessoaContato? PessoaContato { get; set; }

    public long? NegocioId { get; set; }
    public Negocio? Negocio { get; set; }

    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

public enum TipoAtividade
{
    Tarefa = 0,
    Ligacao = 1,
    Email = 2,
    Reuniao = 3,
    Nota = 4
}

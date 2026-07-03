namespace AST.Comercial.Domain.Entidades;

public class PessoaContato : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Cargo { get; set; }
    public string? Documento { get; set; } // CPF
    public string? Observacoes { get; set; }
    public bool Decisor { get; set; }

    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public ICollection<Atividade> Atividades { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

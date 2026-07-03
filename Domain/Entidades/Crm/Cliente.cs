namespace AST.Comercial.Domain.Entidades;

public class Cliente : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Documento { get; set; } // CNPJ
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Endereco { get; set; }
    public string? Cep { get; set; }
    public string? Observacoes { get; set; }
    public string? Site { get; set; }

    public long? StatusClienteId { get; set; }
    public StatusCliente? StatusCliente { get; set; }

    public long? OrigemId { get; set; }
    public OrigemContato? Origem { get; set; }

    public long? RamoAtividadeId { get; set; }
    public RamoAtividade? RamoAtividade { get; set; }

    public long? PorteEmpresaId { get; set; }
    public PorteEmpresa? PorteEmpresa { get; set; }

    public ICollection<PessoaContato> Contatos { get; set; } = [];
    public ICollection<Negocio> Negocios { get; set; } = [];
    public ICollection<Atividade> Atividades { get; set; } = [];
    public ICollection<ValorCampo> OutrasPropriedades { get; set; } = [];
}

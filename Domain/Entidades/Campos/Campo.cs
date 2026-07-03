using System.Text.Json;

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Campo personalizado (equivalente ao Field da Ploomes).
/// Define a estrutura de um campo customizável para qualquer entidade.
/// </summary>
public class Campo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Chave única gerada automaticamente: EntidadeAlvo_GUID (ex: Negocio_3FE11CCBD3A74EBFA3022D4FEB8100DC)
    /// </summary>
    public string Chave { get; set; } = string.Empty;

    public void GerarChave()
    {
        Chave = $"{EntidadeAlvo}_{Guid.NewGuid():N}";
    }
    public string? Descricao { get; set; }
    public TipoCampo Tipo { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }

    // Configurações de exibição
    public int Ordem { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Visivel { get; set; } = true;
    public bool SomenteLeitura { get; set; }

    // Valor padrão (JSON)
    public JsonElement? ValorPadrao { get; set; }

    // Opções para campos do tipo Lista/MultiLista
    public ICollection<OpcaoCampo> Opcoes { get; set; } = [];

    // Regras de visibilidade/obrigatoriedade condicional
    public ICollection<RegraCampo> Regras { get; set; } = [];

    // Fórmula JavaScript segura (ex: retorna valor calculado)
    public string? Formula { get; set; }

    // Validações customizadas
    public int? TamanhoMinimo { get; set; }
    public int? TamanhoMaximo { get; set; }
    public decimal? ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }
    public string? Mascara { get; set; }

    // Vinculação a etapa específica do funil (se aplicável)
    public long? EtapaFunilId { get; set; }
    public EtapaFunil? EtapaFunil { get; set; }
}

/// <summary>
/// Tipo do campo personalizado
/// </summary>
public enum TipoCampo
{
    Texto = 0,
    TextoLongo = 1,
    Numero = 2,
    Decimal = 3,
    Moeda = 4,
    Data = 5,
    DataHora = 6,
    Booleano = 7,
    Lista = 8,
    MultiLista = 9,
    Email = 10,
    Telefone = 11,
    Url = 12,
    Cpf = 13,
    Cnpj = 14,
    Cep = 15,
    Formula = 16,
    Referencia = 17
}

/// <summary>
/// Entidade à qual o campo está vinculado
/// </summary>
public enum EntidadeAlvo
{
    Cliente = 0,
    Negocio = 1,
    Funil = 2,
    EtapaFunil = 3,
    Atividade = 4,
    Produto = 5,
    Empresa = 6,
    Usuario = 7,
    Equipe = 8,
    Departamento = 9,
    Cargo = 10,
    Proposta = 11,
    ItemProposta = 12,
    SecaoProposta = 13,
    ParcelaProposta = 14,
    PessoaContato = 15
}





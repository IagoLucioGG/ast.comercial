namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Modelo/Template de documento (proposta comercial, contrato, etc.)
/// Contém HTML com placeholders dinâmicos usando sintaxe $entidade.campo.
/// 
/// Placeholders:
///   $proposta.Valor, $cliente.Nome, $item.Adesao, $bloco.TotalAdesao
/// 
/// Condicionais (controla visibilidade no PDF):
///   &lt;span data-if="$proposta.TotalImplantacao > 0"&gt;...&lt;/span&gt;
/// 
/// Loops:
///   &lt;tbody data-each="$proposta.Itens"&gt;...&lt;/tbody&gt;
///   &lt;tbody data-each="$proposta.Itens" data-filter="GrupoProduto eq 'Software'"&gt;...&lt;/tbody&gt;
/// </summary>
public class ModeloDocumento : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; } = EntidadeAlvo.Proposta;
    public bool Padrao { get; set; }
    public int Ordem { get; set; }

    /// <summary>
    /// Quem pode ver/usar este modelo (null = todos da empresa)
    /// </summary>
    public long? EquipeId { get; set; }
    public Equipe? Equipe { get; set; }

    public ICollection<SecaoModelo> Secoes { get; set; } = [];
}

/// <summary>
/// Seção/aba dentro de um modelo de documento.
/// Cada seção é uma "página" ou bloco do template (Capa, Produtos, Condições, etc.)
/// </summary>
public class SecaoModelo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }

    /// <summary>
    /// Conteúdo HTML do template com placeholders $entidade.campo
    /// </summary>
    public string ConteudoHtml { get; set; } = string.Empty;

    public long ModeloDocumentoId { get; set; }
    public ModeloDocumento ModeloDocumento { get; set; } = null!;
}

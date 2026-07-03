namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Configuração de comportamento de qualquer campo (fixo ou personalizado).
/// Define fórmulas de visibilidade, obrigatoriedade, cálculo e valor padrão.
/// É da empresa e reutilizável: o mesmo campo pode ser vinculado a vários funis/etapas/modelos.
/// Os valores preenchidos são exclusivos de cada registro (ValorCampo), mas a configuração é compartilhada.
/// </summary>
public class ConfiguracaoCampo : EntidadeBase
{
    /// <summary>
    /// Entidade à qual esta configuração se aplica
    /// </summary>
    public EntidadeAlvo EntidadeAlvo { get; set; }

    /// <summary>
    /// Nome do campo fixo (ex: "Valor", "Status", "Titulo") ou null se for campo personalizado
    /// </summary>
    public string? NomeCampoFixo { get; set; }

    /// <summary>
    /// Referência ao Campo personalizado (null se for campo fixo)
    /// </summary>
    public long? CampoId { get; set; }
    public Campo? Campo { get; set; }

    // Exibição
    public int Ordem { get; set; }
    public string? Rotulo { get; set; }
    public string? Placeholder { get; set; }
    public string? Dica { get; set; }
    public string? Agrupamento { get; set; }
    public int? Largura { get; set; }

    // Fórmulas JS (executadas no sandbox)
    public string? FormulaVisibilidade { get; set; }
    public string? FormulaObrigatoriedade { get; set; }
    public string? FormulaCalculo { get; set; }
    public string? FormulaValorPadrao { get; set; }
    public string? FormulaSomenteLeitura { get; set; }

    // Configurações estáticas (quando não precisa de fórmula)
    public bool Obrigatorio { get; set; }
    public bool Visivel { get; set; } = true;
    public bool SomenteLeitura { get; set; }

    // Vinculações opcionais (reutilização em contextos específicos)
    public long? FunilId { get; set; }
    public Funil? Funil { get; set; }

    public long? EtapaFunilId { get; set; }
    public EtapaFunil? EtapaFunil { get; set; }

    public long? PropostaTemplateId { get; set; }
}

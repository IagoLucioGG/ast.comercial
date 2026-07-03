namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Formulário configurável — define quais campos aparecem e como, para cada entidade.
/// O admin pode criar vários formulários para a mesma entidade (ex: "Empresas", "Empresas (mini)", "Pessoas").
/// Cada formulário tem seções com campos ordenados.
/// </summary>
public class Formulario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public EntidadeAlvo EntidadeAlvo { get; set; }
    public bool Padrao { get; set; }
    public int Ordem { get; set; }

    public ICollection<SecaoFormulario> Secoes { get; set; } = [];
    public ICollection<CampoFormulario> Campos { get; set; } = [];
}

/// <summary>
/// Seção dentro de um formulário (agrupamento visual de campos).
/// Ex: "Dados Básicos", "Endereço", "Informações Comerciais"
/// </summary>
public class SecaoFormulario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int? Colunas { get; set; } = 2;
    public bool Colapsavel { get; set; }
    public bool IniciaColapsada { get; set; }

    public long FormularioId { get; set; }
    public Formulario Formulario { get; set; } = null!;

    public ICollection<CampoFormulario> Campos { get; set; } = [];
}

/// <summary>
/// Um campo dentro de uma seção do formulário.
/// Define qual campo aparece, em qual posição, com qual comportamento neste form específico.
/// </summary>
public class CampoFormulario : EntidadeBase
{
    public long? SecaoFormularioId { get; set; }
    public SecaoFormulario? SecaoFormulario { get; set; }

    public long FormularioId { get; set; }
    public Formulario Formulario { get; set; } = null!;

    /// <summary>
    /// Campo fixo da entidade (ex: "Nome", "Email", "Valor"). Null se for campo personalizado.
    /// </summary>
    public string? NomeCampoFixo { get; set; }

    /// <summary>
    /// Campo personalizado. Null se for campo fixo.
    /// </summary>
    public long? CampoId { get; set; }
    public Campo? Campo { get; set; }

    // Posicionamento
    public int Ordem { get; set; }
    public int? Coluna { get; set; }
    public int? LarguraColunas { get; set; }

    // Aparência
    public string? Rotulo { get; set; }
    public string? Placeholder { get; set; }
    public string? Dica { get; set; }
    public string? Icone { get; set; }

    // Comportamento estático
    public bool Visivel { get; set; } = true;
    public bool Obrigatorio { get; set; }
    public bool SomenteLeitura { get; set; }

    // Fórmulas JS dinâmicas
    public string? FormulaVisibilidade { get; set; }
    public string? FormulaObrigatoriedade { get; set; }
    public string? FormulaCalculo { get; set; }
    public string? FormulaValorPadrao { get; set; }
    public string? FormulaSomenteLeitura { get; set; }
}

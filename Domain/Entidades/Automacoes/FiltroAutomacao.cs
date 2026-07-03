namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Condição/Filtro de uma automação.
/// Filtros no mesmo grupo = E (AND). Grupos diferentes = OU (OR).
/// </summary>
public class FiltroAutomacao : EntidadeBase
{
    public long AutomacaoId { get; set; }
    public Automacao Automacao { get; set; } = null!;

    /// <summary>
    /// Grupo lógico — filtros no mesmo grupo são AND, grupos diferentes são OR
    /// </summary>
    public int Grupo { get; set; }
    public int Ordem { get; set; }

    /// <summary>
    /// Referência ao campo: nome fixo (ex: "Nome", "Valor") ou chave personalizada (ex: "Negocio_abc123").
    /// Suporta navegação (ex: "Contato.Tipo", "Funil.Nome")
    /// </summary>
    public string CampoReferencia { get; set; } = string.Empty;

    public OperadorFiltro Operador { get; set; }

    /// <summary>
    /// Valor de comparação (string — convertido no runtime para o tipo correto)
    /// </summary>
    public string? Valor { get; set; }

    /// <summary>
    /// Para operador MudouDe/MudouPara — valor anterior
    /// </summary>
    public string? ValorAnterior { get; set; }
}

public enum OperadorFiltro
{
    Igual = 0,
    Diferente = 1,
    Contem = 2,
    NaoContem = 3,
    Maior = 4,
    Menor = 5,
    MaiorOuIgual = 6,
    MenorOuIgual = 7,
    Vazio = 8,
    NaoVazio = 9,
    MudouPara = 10,
    MudouDe = 11,
    ComecaCom = 12,
    TerminaCom = 13
}

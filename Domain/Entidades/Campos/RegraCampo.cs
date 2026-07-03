using System.Text.Json;

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Regra condicional de um campo.
/// Define quando um campo fica visível, obrigatório, etc., com base no valor de outro campo.
/// </summary>
public class RegraCampo : EntidadeBase
{
    public long CampoId { get; set; }
    public Campo Campo { get; set; } = null!;

    // Campo que dispara a regra
    public long CampoOrigemId { get; set; }
    public Campo CampoOrigem { get; set; } = null!;

    public OperadorRegra Operador { get; set; }

    // Valor de comparação (JSON para suportar qualquer tipo)
    public JsonElement? ValorComparacao { get; set; }

    // Ações da regra
    public AcaoRegra Acao { get; set; }

    // Fórmula JS para regras complexas (ex: campo1 > 10 && campo2 == "ativo")
    public string? FormulaCondicao { get; set; }
}

public enum OperadorRegra
{
    Igual = 0,
    Diferente = 1,
    Maior = 2,
    Menor = 3,
    MaiorOuIgual = 4,
    MenorOuIgual = 5,
    Contem = 6,
    NaoContem = 7,
    Vazio = 8,
    NaoVazio = 9,
    FormulaCustomizada = 10
}

public enum AcaoRegra
{
    Mostrar = 0,
    Esconder = 1,
    TornarObrigatorio = 2,
    TornarOpcional = 3,
    TornarSomenteLeitura = 4,
    TornarEditavel = 5,
    DefinirValor = 6
}





using System.Text.Json;
namespace AST.Comercial.Application.Dtos;

// ===== Painéis e gráficos =====
public class FiltroRelatorioDto
{
    public string Campo { get; set; } = string.Empty;
    public string Operador { get; set; } = "eq";
    public JsonElement? Valor { get; set; }
}

/// <summary>Definição de um gráfico (usada tanto para salvar quanto para consultar dados).</summary>
public class DefinicaoGraficoDto
{
    public string Entidade { get; set; } = string.Empty;
    public string Agregacao { get; set; } = "Contagem";
    public string? CampoValor { get; set; }
    public string? CampoAgrupamento { get; set; }
    public List<FiltroRelatorioDto> Filtros { get; set; } = [];
    public string? PeriodoCampo { get; set; }
    public string? PeriodoRelativo { get; set; }
    /// <summary>Quantidade máxima de itens do agrupamento (1 a 100).</summary>
    public int? Limite { get; set; }
}

/// <summary>Corpo de POST api/relatorios/dados.</summary>
public class ConsultaDadosDto : DefinicaoGraficoDto
{
    public string? Tipo { get; set; }
    /// <summary>Período do painel: sobrepõe o período relativo do gráfico.</summary>
    public string? PeriodoPainel { get; set; }
    public DateTime? PeriodoInicio { get; set; }
    public DateTime? PeriodoFim { get; set; }
}

public class GraficoEntradaDto : DefinicaoGraficoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Barras";
    public int Largura { get; set; } = 2;
}

public class GraficoSaidaDto : GraficoEntradaDto
{
    public long Id { get; set; }
    public long PainelId { get; set; }
    public int Ordem { get; set; }
}

public class PainelEntradaDto
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public bool? Compartilhado { get; set; }
}

public class PainelSaidaDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Compartilhado { get; set; }
    public int Ordem { get; set; }
    public long? CriadoPorUsuarioId { get; set; }
    public string? CriadoPorNome { get; set; }
    public bool PodeEditar { get; set; }
    public int QuantidadeGraficos { get; set; }
    public List<GraficoSaidaDto>? Graficos { get; set; }
}

public class ReordenarDto
{
    public List<long> Ids { get; set; } = [];
}

// ===== Dados prontos =====
public class MoedaSerieDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Simbolo { get; set; }
}

public class PontoDadosDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}

public class SerieDadosDto
{
    /// <summary>Moeda da série (nula quando o valor não é monetário).</summary>
    public MoedaSerieDto? Moeda { get; set; }
    public List<PontoDadosDto> Pontos { get; set; } = [];
    /// <summary>Total da série (nulo para média com agrupamento).</summary>
    public decimal? Total { get; set; }
    public int Quantidade { get; set; }
}

public class RespostaDadosDto
{
    public string Entidade { get; set; } = string.Empty;
    public string Agregacao { get; set; } = string.Empty;
    public string? CampoValor { get; set; }
    public string? CampoAgrupamento { get; set; }
    /// <summary>"mes", "id", "texto" ou nulo (sem agrupamento).</summary>
    public string? TipoAgrupamento { get; set; }
    public bool Monetario { get; set; }
    public bool Truncado { get; set; }
    public DateTime? PeriodoInicio { get; set; }
    public DateTime? PeriodoFim { get; set; }
    public List<SerieDadosDto> Series { get; set; } = [];
}

// ===== Catálogo =====
public class OpcaoRelDto
{
    public string Valor { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
}

public class CampoCatalogoDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    /// <summary>id, numero, data, texto, bool, enum, mes.</summary>
    public string Tipo { get; set; } = string.Empty;
    public bool Monetario { get; set; }
    /// <summary>Para campos "id": nome da referência para GET api/relatorios/opcoes/{referencia}.</summary>
    public string? Referencia { get; set; }
    public List<string> Operadores { get; set; } = [];
    public List<OpcaoRelDto>? Opcoes { get; set; }
}

public class EntidadeCatalogoDto
{
    public string Chave { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
    public List<string> Agregacoes { get; set; } = [];
    public List<CampoCatalogoDto> CamposValor { get; set; } = [];
    public List<CampoCatalogoDto> CamposAgrupamento { get; set; } = [];
    public List<CampoCatalogoDto> CamposFiltro { get; set; } = [];
    public List<CampoCatalogoDto> CamposPeriodo { get; set; } = [];
    public string? PeriodoCampoPadrao { get; set; }
}

public class CatalogoRelatorioDto
{
    public List<EntidadeCatalogoDto> Entidades { get; set; } = [];
    public List<OpcaoRelDto> Periodos { get; set; } = [];
    public List<OpcaoRelDto> Tipos { get; set; } = [];
    public List<OpcaoRelDto> Operadores { get; set; } = [];
}

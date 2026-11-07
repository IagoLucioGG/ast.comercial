using System.Text.Json;

namespace AST.Comercial.Application.Dtos;

/// <summary>Corpo de criação/edição de automação (contrato em mapeamento/analise/07-contrato-automacoes.md).</summary>
public class AutomacaoEntradaDto
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public string? EntidadeAlvo { get; set; }
    public string? Gatilho { get; set; }
    public int Ordem { get; set; }
    public bool ImpedirCascata { get; set; }
    public bool? Ativo { get; set; }
    public string? HorarioExecucao { get; set; }
    // Recebidos apenas para rejeitar explicitamente (motor não suporta)
    public string? CronExpressao { get; set; }
    public string? FormulaCondicao { get; set; }
    public string? GatilhoCampoReferencia { get; set; }
    public List<FiltroAutomacaoEntradaDto>? Filtros { get; set; }
    public List<AcaoAutomacaoEntradaDto>? Acoes { get; set; }
}

public class FiltroAutomacaoEntradaDto
{
    public int Grupo { get; set; }
    public string? CampoReferencia { get; set; }
    public string? Operador { get; set; }
    public string? Valor { get; set; }
}

public class AcaoAutomacaoEntradaDto
{
    public string? Tipo { get; set; }
    public JsonElement? Configuracao { get; set; }
}

public class AutomacaoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string EntidadeAlvo { get; set; } = string.Empty;
    public string Gatilho { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool ImpedirCascata { get; set; }
    public bool Ativo { get; set; }
    public string? HorarioExecucao { get; set; }
    public DateTime? ProximaExecucao { get; set; }
    public DateTime? UltimaExecucao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public long? CriadoPorUsuarioId { get; set; }
    /// <summary>Nome do usuário que criou a automação (null em automações antigas ou criadas por sistema).</summary>
    public string? CriadoPor { get; set; }
    public List<FiltroAutomacaoDto> Filtros { get; set; } = [];
    public List<AcaoAutomacaoDto> Acoes { get; set; } = [];
}

public class FiltroAutomacaoDto
{
    public long Id { get; set; }
    public int Grupo { get; set; }
    public int Ordem { get; set; }
    public string CampoReferencia { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;
    public string? Valor { get; set; }
}

public class AcaoAutomacaoDto
{
    public long Id { get; set; }
    public int Ordem { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public JsonElement Configuracao { get; set; }
}

public class ExecucaoAutomacaoDto
{
    public long Id { get; set; }
    public long AutomacaoId { get; set; }
    public long? RegistroId { get; set; }
    public string EntidadeAlvo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime IniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
    public int DuracaoMs { get; set; }
    public string? Detalhes { get; set; }
    public string? Erro { get; set; }
    public string? DisparadoPor { get; set; }
}

public class ListaPaginadaDto<T>
{
    public int Total { get; set; }
    public List<T> Itens { get; set; } = [];
}

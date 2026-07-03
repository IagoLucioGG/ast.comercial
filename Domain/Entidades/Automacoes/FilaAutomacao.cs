namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Fila de automações pendentes de execução (persistida no banco).
/// O processador background consome itens pendentes e executa.
/// Visível via API para monitoramento em tempo real.
/// </summary>
public class FilaAutomacao
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }

    public long AutomacaoId { get; set; }
    public Automacao Automacao { get; set; } = null!;

    public long? RegistroId { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }
    public TipoGatilho Gatilho { get; set; }

    public StatusFila Status { get; set; } = StatusFila.Pendente;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }

    public int Tentativas { get; set; }
    public int MaxTentativas { get; set; } = 3;
    public DateTime? ProximaTentativa { get; set; }
    public string? UltimoErro { get; set; }

    /// <summary>
    /// Snapshot do registro no momento do evento (para contexto da execução)
    /// </summary>
    public string? DadosEvento { get; set; }

    public string? DisparadoPor { get; set; }

    /// <summary>
    /// Se foi disparada por outra automação (controle de cascata)
    /// </summary>
    public long? AutomacaoOrigemId { get; set; }
}

public enum StatusFila
{
    Pendente = 0,
    EmExecucao = 1,
    Concluido = 2,
    Falha = 3,
    FalhaDefinitiva = 4,
    Cancelado = 5
}

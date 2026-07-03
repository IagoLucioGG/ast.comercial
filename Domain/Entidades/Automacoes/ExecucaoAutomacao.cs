namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Registro de execução de uma automação (histórico/auditoria).
/// Armazena se executou com sucesso, qual registro disparou, quanto tempo levou.
/// </summary>
public class ExecucaoAutomacao
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }

    public long AutomacaoId { get; set; }
    public Automacao Automacao { get; set; } = null!;

    /// <summary>
    /// Registro que disparou a automação
    /// </summary>
    public long? RegistroId { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }

    public StatusExecucao Status { get; set; }
    public DateTime IniciadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizadaEm { get; set; }
    public int DuracaoMs { get; set; }

    /// <summary>
    /// Detalhes da execução (quais ações rodaram, erros, etc.)
    /// </summary>
    public string? Detalhes { get; set; }

    /// <summary>
    /// Mensagem de erro (se falhou)
    /// </summary>
    public string? Erro { get; set; }

    /// <summary>
    /// Usuário que disparou a ação original (ou "Sistema" para periódicas)
    /// </summary>
    public string? DisparadoPor { get; set; }
}

public enum StatusExecucao
{
    EmExecucao = 0,
    Sucesso = 1,
    Falha = 2,
    FalhaFiltro = 3,
    Cancelada = 4
}

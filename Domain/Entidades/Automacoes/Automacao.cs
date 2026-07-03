namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Automação configurável pelo admin.
/// Define: QUANDO (gatilho) + SE (filtros) + ENTÃO (ações).
/// Executa no backend automaticamente.
/// </summary>
public class Automacao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }
    public TipoGatilho Gatilho { get; set; }
    public int Ordem { get; set; }

    /// <summary>
    /// Se true, não é disparada por ações de outras automações (evita loop)
    /// </summary>
    public bool ImpedirCascata { get; set; }

    /// <summary>
    /// Gatilho CampoAlterado: qual campo dispara (nome fixo ou chave do personalizado)
    /// </summary>
    public string? GatilhoCampoReferencia { get; set; }

    /// <summary>
    /// Gatilho Periodicamente: expressão cron (ex: "0 9 * * 1-5" = 9h dias úteis)
    /// </summary>
    public string? CronExpressao { get; set; }
    public TimeOnly? HorarioExecucao { get; set; }
    public DateTime? ProximaExecucao { get; set; }
    public DateTime? UltimaExecucao { get; set; }

    /// <summary>
    /// Condição JS opcional para filtros complexos (ex: $registro.Valor > 10000 && $campos.Negocio_abc != null)
    /// </summary>
    public string? FormulaCondicao { get; set; }

    public ICollection<FiltroAutomacao> Filtros { get; set; } = [];
    public ICollection<AcaoAutomacao> Acoes { get; set; } = [];
}

/// <summary>
/// Quando a automação deve ser disparada
/// </summary>
public enum TipoGatilho
{
    Criado = 0,
    Editado = 1,
    CampoAlterado = 2,
    Removido = 3,
    EtapaMudou = 4,
    StatusMudou = 5,
    Concluido = 6,
    Vencido = 7,
    PropostaCriada = 8,
    PropostaAprovada = 9,
    PropostaEnviada = 10,
    PropostaExpirada = 11,
    EtiquetaAdicionada = 12,
    Periodicamente = 13
}

namespace AST.Comercial.Application.Interfaces;

/// <summary>
/// Abstração para publicar eventos na fila de automações.
/// Hoje: insere no banco + pg_notify.
/// Futuro: pode publicar em RabbitMQ/SQS para microserviço separado.
/// </summary>
public interface IFilaAutomacaoPublicador
{
    Task PublicarEventoAsync(EventoAutomacao evento, CancellationToken cancellationToken = default);
}

public record EventoAutomacao
{
    public long EmpresaId { get; init; }
    public long RegistroId { get; init; }
    public string EntidadeAlvo { get; init; } = string.Empty;
    public string Gatilho { get; init; } = string.Empty;
    public string? DadosEvento { get; init; }
    public string? DisparadoPor { get; init; }
    public long? AutomacaoOrigemId { get; init; }
}

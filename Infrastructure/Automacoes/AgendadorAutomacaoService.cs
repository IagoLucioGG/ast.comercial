using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Automacoes;

/// <summary>
/// Background service para automações periódicas (cron).
/// Verifica a cada minuto se há automações com ProximaExecucao <= agora.
/// Insere na fila para o processador executar.
/// </summary>
public class AgendadorAutomacaoService(
    IServiceScopeFactory scopeFactory,
    ILogger<AgendadorAutomacaoService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Agendador de automações periódicas iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarAgendamentosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no agendador de automações");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task VerificarAgendamentosAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agora = DateTime.UtcNow;
        var automacoes = await db.Automacoes
            .IgnoreQueryFilters()
            .Where(a => a.Ativo
                && a.Gatilho == TipoGatilho.Periodicamente
                && a.ProximaExecucao != null
                && a.ProximaExecucao <= agora)
            .ToListAsync(cancellationToken);

        if (automacoes.Count == 0) return;

        logger.LogInformation("Agendando {Count} automações periódicas", automacoes.Count);

        foreach (var automacao in automacoes)
        {
            db.FilaAutomacao.Add(new FilaAutomacao
            {
                EmpresaId = automacao.EmpresaId,
                AutomacaoId = automacao.Id,
                EntidadeAlvo = automacao.EntidadeAlvo,
                Gatilho = TipoGatilho.Periodicamente,
                DisparadoPor = "Sistema (Agendador)"
            });

            // Calcular próxima execução baseado no cron/horário
            automacao.UltimaExecucao = agora;
            automacao.ProximaExecucao = CalcularProximaExecucao(automacao);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Notificar processador
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_notify('fila_automacao', @p0)",
            [automacoes.Count.ToString()],
            cancellationToken);
    }

    private static DateTime? CalcularProximaExecucao(Automacao automacao)
    {
        // TODO: implementar parser de cron completo (Cronos library)
        // Por enquanto, se tem HorarioExecucao, agenda para o mesmo horário do dia seguinte
        if (automacao.HorarioExecucao is not null)
        {
            var amanha = DateTime.UtcNow.Date.AddDays(1);
            return amanha.Add(automacao.HorarioExecucao.Value.ToTimeSpan());
        }

        // Fallback: próxima execução em 24h
        return DateTime.UtcNow.AddHours(24);
    }
}

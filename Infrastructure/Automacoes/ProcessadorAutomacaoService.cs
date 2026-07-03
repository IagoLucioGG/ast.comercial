using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AST.Comercial.Infrastructure.Automacoes;

/// <summary>
/// Background service que processa automações da fila.
/// Usa PostgreSQL LISTEN/NOTIFY para reagir imediatamente (sem polling).
/// Fallback: polling a cada 5s caso a notificação falhe.
/// Preparado para microserviço: basta rodar em outro processo com mesmo banco.
/// </summary>
public class ProcessadorAutomacaoService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessadorAutomacaoService> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int FallbackPollingMs = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Processador de automações iniciado");

        await using var listenConn = await CriarConexaoListenAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await listenConn.WaitAsync(TimeSpan.FromMilliseconds(FallbackPollingMs), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao aguardar notificação");
                await Task.Delay(2000, stoppingToken);
                continue;
            }

            await ProcessarLoteAsync(stoppingToken);
        }
    }

    private async Task ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var motor = scope.ServiceProvider.GetRequiredService<IMotorAutomacao>();

        var pendentes = await db.FilaAutomacao
            .FromSqlRaw("""
                SELECT * FROM "FilaAutomacao"
                WHERE "Status" = 'Pendente'
                  AND ("ProximaTentativa" IS NULL OR "ProximaTentativa" <= NOW())
                ORDER BY "CriadoEm"
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
                """, BatchSize)
            .ToListAsync(cancellationToken);

        if (pendentes.Count == 0) return;

        logger.LogInformation("Processando {Count} automações", pendentes.Count);

        foreach (var item in pendentes)
        {
            item.Status = StatusFila.EmExecucao;
            item.IniciadoEm = DateTime.UtcNow;
            item.Tentativas++;
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in pendentes)
        {
            try
            {
                await motor.ProcessarItemFilaAsync(item.Id, cancellationToken);
                item.Status = StatusFila.Concluido;
                item.FinalizadoEm = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro automação {Id} registro {Registro}", item.AutomacaoId, item.RegistroId);
                item.UltimoErro = ex.Message[..Math.Min(ex.Message.Length, 2000)];

                if (item.Tentativas >= item.MaxTentativas)
                {
                    item.Status = StatusFila.FalhaDefinitiva;
                    item.FinalizadoEm = DateTime.UtcNow;
                }
                else
                {
                    item.Status = StatusFila.Falha;
                    item.ProximaTentativa = DateTime.UtcNow.AddSeconds(10 * item.Tentativas * item.Tentativas);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> CriarConexaoListenAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connString = db.Database.GetConnectionString();

        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "LISTEN fila_automacao";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("LISTEN fila_automacao ativo");
        return conn;
    }
}

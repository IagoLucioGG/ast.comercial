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

        // A fila (FilaAutomacao) não tem filtro de tenant e carrega EmpresaId por item: a "reivindicação" dos itens
        // é feita numa transação (FOR UPDATE SKIP LOCKED só vale dentro dela) e marca-os EmExecucao antes do commit,
        // evitando que duas instâncias processem o mesmo item.
        // Elegíveis: Pendente, Falha com ProximaTentativa vencida (reprocesso, respeitando MaxTentativas)
        // e EmExecucao "órfão" há mais de 10 min (processo caiu no meio).
        List<FilaAutomacao> pendentes;
        await using (var tx = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            pendentes = await db.FilaAutomacao
                .FromSqlRaw("""
                    SELECT * FROM "FilaAutomacao"
                    WHERE "Tentativas" < "MaxTentativas"
                      AND (
                        ("Status" IN ('Pendente', 'Falha')
                           AND ("ProximaTentativa" IS NULL OR "ProximaTentativa" <= NOW()))
                        OR ("Status" = 'EmExecucao' AND "IniciadoEm" < NOW() - INTERVAL '10 minutes')
                      )
                    ORDER BY "CriadoEm"
                    LIMIT {0}
                    FOR UPDATE SKIP LOCKED
                    """, BatchSize)
                .ToListAsync(cancellationToken);

            if (pendentes.Count == 0) return;

            foreach (var item in pendentes)
            {
                item.Status = StatusFila.EmExecucao;
                item.IniciadoEm = DateTime.UtcNow;
                item.Tentativas++;
            }
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Processando {Count} automações", pendentes.Count);

        db.ChangeTracker.Clear();

        foreach (var item in pendentes)
        {
            var id = item.Id;
            try
            {
                // Cada item roda em escopo próprio (DbContext novo): o motor define EmpresaIdAtual = item.EmpresaId
                // e esse tenant nunca "vaza" para o item seguinte, que pode ser de outra empresa.
                using (var escopoItem = scopeFactory.CreateScope())
                {
                    var motor = escopoItem.ServiceProvider.GetRequiredService<IMotorAutomacao>();
                    await motor.ProcessarItemFilaAsync(id, cancellationToken);
                }

                // O motor pode ter marcado Cancelado (automação removida/desativada): só conclui se ainda EmExecucao
                var agora = DateTime.UtcNow;
                await db.FilaAutomacao
                    .Where(f => f.Id == id && f.Status == StatusFila.EmExecucao)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(f => f.Status, StatusFila.Concluido)
                        .SetProperty(f => f.FinalizadoEm, agora)
                        .SetProperty(f => f.ProximaTentativa, (DateTime?)null), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro automação {Id} registro {Registro}", item.AutomacaoId, item.RegistroId);
                var erro = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                var agora = DateTime.UtcNow;
                var definitiva = item.Tentativas >= item.MaxTentativas;
                // Volta a ser elegível quando ProximaTentativa vencer (backoff 10s, 40s, 90s, ...)
                DateTime? proxima = definitiva ? null : agora.AddSeconds(10 * item.Tentativas * item.Tentativas);
                DateTime? finalizado = definitiva ? agora : null;
                var status = definitiva ? StatusFila.FalhaDefinitiva : StatusFila.Falha;

                try
                {
                    await db.FilaAutomacao
                        .Where(f => f.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(f => f.Status, status)
                            .SetProperty(f => f.UltimoErro, erro)
                            .SetProperty(f => f.ProximaTentativa, proxima)
                            .SetProperty(f => f.FinalizadoEm, finalizado), cancellationToken);
                }
                catch (Exception ex2) when (ex2 is not OperationCanceledException)
                {
                    // Se nem isso salvar, o item fica EmExecucao e é retomado após 10 min (órfão)
                    logger.LogError(ex2, "Falha ao salvar o status do item de fila {Id}", id);
                }
            }
        }
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

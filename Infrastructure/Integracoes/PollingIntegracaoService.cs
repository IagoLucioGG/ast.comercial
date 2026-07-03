using System.Text.Json;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Integracoes;

/// <summary>
/// Serviço em background que executa polling em integrações configuradas com ModoEntrada=Polling.
/// Roda a cada minuto, verifica quais fluxos estão pendentes e sincroniza dados externos.
/// </summary>
public class PollingIntegracaoService(IServiceScopeFactory scopeFactory, ILogger<PollingIntegracaoService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarFluxosPendentesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no ciclo de polling de integrações");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task ProcessarFluxosPendentesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agora = DateTime.UtcNow;

        var fluxos = await db.FluxosIntegracao
            .Include(f => f.Integracao)
            .Where(f => f.Ativo
                && f.Direcao == DirecaoFluxo.Entrada
                && f.ModoEntrada == ModoEntrada.Polling
                && f.IntervaloPollingMinutos != null
                && f.Integracao.Ativo
                && f.Integracao.Status == StatusIntegracao.Ligado
                && (f.UltimaSincronizacao == null
                    || f.UltimaSincronizacao.Value.AddMinutes(f.IntervaloPollingMinutos!.Value) <= agora))
            .ToListAsync(ct);

        foreach (var fluxo in fluxos)
        {
            try
            {
                await ExecutarPollingFluxoAsync(db, fluxo, ct);
                fluxo.UltimaSincronizacao = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no polling do fluxo {FluxoId} (integração {IntegracaoId})", fluxo.Id, fluxo.IntegracaoId);
                db.LogsIntegracao.Add(new LogIntegracao
                {
                    EmpresaId = fluxo.EmpresaId,
                    IntegracaoId = fluxo.IntegracaoId,
                    FluxoIntegracaoId = fluxo.Id,
                    Direcao = DirecaoFluxo.Entrada,
                    Sucesso = false,
                    Endpoint = fluxo.EndpointPolling,
                    MetodoHttp = fluxo.MetodoHttpPolling ?? "GET",
                    Erro = ex.Message
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task ExecutarPollingFluxoAsync(AppDbContext db, FluxoIntegracao fluxo, CancellationToken ct)
    {
        var integracao = fluxo.Integracao;
        if (string.IsNullOrEmpty(fluxo.EndpointPolling)) return;

        using var client = CriarHttpClient(integracao);
        var url = MontarUrl(integracao, fluxo.EndpointPolling);

        var metodo = new HttpMethod(fluxo.MetodoHttpPolling ?? "GET");
        var request = new HttpRequestMessage(metodo, url);

        var response = await client.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        db.LogsIntegracao.Add(new LogIntegracao
        {
            EmpresaId = fluxo.EmpresaId,
            IntegracaoId = integracao.Id,
            FluxoIntegracaoId = fluxo.Id,
            Direcao = DirecaoFluxo.Entrada,
            Sucesso = response.IsSuccessStatusCode,
            Endpoint = url,
            MetodoHttp = metodo.Method,
            StatusCode = (int)response.StatusCode,
            ResponseBody = responseBody
        });

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Polling falhou para fluxo {FluxoId}: HTTP {StatusCode}", fluxo.Id, (int)response.StatusCode);
            return;
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Navegar até o array de itens usando CampoListaResponse
        var itensElement = root;
        if (!string.IsNullOrEmpty(fluxo.CampoListaResponse))
        {
            foreach (var parte in fluxo.CampoListaResponse.Split('.'))
            {
                if (itensElement.ValueKind != JsonValueKind.Object || !itensElement.TryGetProperty(parte, out var next))
                {
                    logger.LogWarning("CampoListaResponse '{Campo}' não encontrado no response do fluxo {FluxoId}", fluxo.CampoListaResponse, fluxo.Id);
                    return;
                }
                itensElement = next;
            }
        }

        if (itensElement.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("Response do polling do fluxo {FluxoId} não é um array", fluxo.Id);
            return;
        }

        // Carregar mapeamentos
        var mapeamentos = await db.MapeamentosIntegracao.AsNoTracking()
            .Where(m => m.IntegracaoId == integracao.Id
                && (m.FluxoIntegracaoId == fluxo.Id || m.FluxoIntegracaoId == null)
                && (m.Direcao == DirecaoMapeamento.Entrada || m.Direcao == DirecaoMapeamento.Ambos))
            .OrderBy(m => m.Ordem)
            .ToListAsync(ct);

        var processados = 0;
        foreach (var item in itensElement.EnumerateArray())
        {
            var dados = ProcessadorMapeamento.ExtrairDadosEntrada(item, mapeamentos);

            // Verificar duplicidade
            long? existenteId = null;
            if (!string.IsNullOrEmpty(fluxo.CampoChaveDuplicidade) && dados.TryGetValue(fluxo.CampoChaveDuplicidade, out var chaveValor) && chaveValor is not null)
            {
                existenteId = await BuscarRegistroExistenteAsync(db, fluxo.EntidadeAlvo, fluxo.CampoChaveDuplicidade, chaveValor.ToString()!, fluxo.EmpresaId, ct);
            }

            var deveSalvar = fluxo.Duplicidade switch
            {
                ComportamentoDuplicidade.Criar when existenteId is not null => false,
                ComportamentoDuplicidade.Atualizar when existenteId is null => false,
                ComportamentoDuplicidade.Ignorar when existenteId is not null => false,
                _ => true
            };

            if (deveSalvar)
            {
                await SalvarEntidadeAsync(db, fluxo.EntidadeAlvo, dados, existenteId, fluxo.EmpresaId, ct);
                processados++;
            }
        }

        logger.LogInformation("Polling do fluxo {FluxoId}: {Processados} registros sincronizados", fluxo.Id, processados);
    }

    private static async Task<long?> BuscarRegistroExistenteAsync(AppDbContext db, EntidadeAlvo entidade, string campo, string valor, long empresaId, CancellationToken ct)
    {
        if (campo.Equals("ChaveExterna", StringComparison.OrdinalIgnoreCase))
        {
            return entidade switch
            {
                EntidadeAlvo.Cliente => (await db.Clientes.FirstOrDefaultAsync(c => c.ChaveExterna == valor && c.EmpresaId == empresaId, ct))?.Id,
                EntidadeAlvo.Negocio => (await db.Negocios.FirstOrDefaultAsync(n => n.ChaveExterna == valor && n.EmpresaId == empresaId, ct))?.Id,
                EntidadeAlvo.Produto => (await db.Produtos.FirstOrDefaultAsync(p => p.ChaveExterna == valor && p.EmpresaId == empresaId, ct))?.Id,
                _ => null
            };
        }
        return null;
    }

    private static async Task SalvarEntidadeAsync(AppDbContext db, EntidadeAlvo entidade, Dictionary<string, object?> dados, long? existenteId, long empresaId, CancellationToken ct)
    {
        switch (entidade)
        {
            case EntidadeAlvo.Cliente:
                var cliente = existenteId is not null
                    ? await db.Clientes.FirstAsync(c => c.Id == existenteId.Value, ct)
                    : CriarEntidade<Cliente>(db, empresaId);
                AplicarDados(cliente, dados);
                break;
            case EntidadeAlvo.Negocio:
                var negocio = existenteId is not null
                    ? await db.Negocios.FirstAsync(n => n.Id == existenteId.Value, ct)
                    : CriarEntidade<Negocio>(db, empresaId);
                AplicarDados(negocio, dados);
                break;
            case EntidadeAlvo.Produto:
                var produto = existenteId is not null
                    ? await db.Produtos.FirstAsync(p => p.Id == existenteId.Value, ct)
                    : CriarEntidade<Produto>(db, empresaId);
                AplicarDados(produto, dados);
                break;
        }

        await db.SaveChangesAsync(ct);
    }

    private static T CriarEntidade<T>(AppDbContext db, long empresaId) where T : EntidadeBase, new()
    {
        var entidade = new T { EmpresaId = empresaId };
        db.Set<T>().Add(entidade);
        return entidade;
    }

    private static void AplicarDados<T>(T entidade, Dictionary<string, object?> dados) where T : class
    {
        var tipo = typeof(T);
        foreach (var (campo, valor) in dados)
        {
            if (valor is null) continue;
            var prop = tipo.GetProperty(campo, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite) continue;

            try
            {
                var tipoAlvo = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                object? valorConvertido;
                if (tipoAlvo == typeof(DateTime) && valor is string str && DateTime.TryParse(str, out var dt))
                    valorConvertido = dt;
                else if (tipoAlvo == typeof(decimal) && valor is decimal dec)
                    valorConvertido = dec;
                else
                    valorConvertido = Convert.ChangeType(valor, tipoAlvo);
                prop.SetValue(entidade, valorConvertido);
            }
            catch
            {
                // Campo incompatível, ignorar
            }
        }
    }

    private static HttpClient CriarHttpClient(Integracao integracao)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        switch (integracao.TipoAuth)
        {
            case TipoAutenticacao.Bearer:
                if (!string.IsNullOrEmpty(integracao.ApiToken))
                    client.DefaultRequestHeaders.Authorization = new("Bearer", integracao.ApiToken);
                break;
            case TipoAutenticacao.BasicAuth:
                if (!string.IsNullOrEmpty(integracao.AuthUsuario))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes($"{integracao.AuthUsuario}:{integracao.AuthSenha}");
                    client.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(bytes));
                }
                break;
            case TipoAutenticacao.ApiKey:
                if (!string.IsNullOrEmpty(integracao.ApiKeyHeader) && !string.IsNullOrEmpty(integracao.ApiKeyValor))
                    client.DefaultRequestHeaders.TryAddWithoutValidation(integracao.ApiKeyHeader, integracao.ApiKeyValor);
                break;
        }

        if (!string.IsNullOrWhiteSpace(integracao.HeadersFixos))
        {
            try
            {
                var headers = JsonDocument.Parse(integracao.HeadersFixos);
                foreach (var h in headers.RootElement.EnumerateObject())
                    client.DefaultRequestHeaders.TryAddWithoutValidation(h.Name, h.Value.GetString());
            }
            catch { }
        }

        return client;
    }

    private static string MontarUrl(Integracao integracao, string endpoint)
        => endpoint.StartsWith("http") ? endpoint : $"{integracao.ApiUrl?.TrimEnd('/')}/{endpoint.TrimStart('/')}";
}

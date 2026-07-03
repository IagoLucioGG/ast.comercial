using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Integracoes;

/// <summary>
/// Executa fluxos de integração de saída com etapas sequenciais, condições e contexto.
/// </summary>
public partial class ExecutorFluxoSaida(AppDbContext db, ILogger<ExecutorFluxoSaida> logger)
{
    public async Task ExecutarAsync(FluxoIntegracao fluxo, Dictionary<string, object?> dados,
        List<Dictionary<string, object?>>? itens, CancellationToken ct = default)
    {
        var integracao = await db.Integracoes.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == fluxo.IntegracaoId, ct);
        if (integracao is null) return;

        var etapas = await db.EtapasFluxoIntegracao.AsNoTracking()
            .Where(e => e.FluxoIntegracaoId == fluxo.Id).OrderBy(e => e.Ordem).ToListAsync(ct);

        if (etapas.Count == 0)
        {
            await ExecutarSimplesAsync(fluxo, integracao, dados, itens, ct);
            return;
        }

        var contexto = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var etapa in etapas)
        {
            if (!AvaliarCondicao(etapa, contexto)) continue;

            var ok = etapa.Tipo == TipoEtapaIntegracao.Consultar
                ? await ConsultarAsync(etapa, integracao, dados, contexto, ct)
                : await EnviarAsync(etapa, integracao, dados, itens, contexto, ct);

            if (!ok && etapa.PararSeErro) break;
        }
    }

    private async Task ExecutarSimplesAsync(FluxoIntegracao fluxo, Integracao integracao,
        Dictionary<string, object?> dados, List<Dictionary<string, object?>>? itens, CancellationToken ct)
    {
        var maps = await db.MapeamentosIntegracao.AsNoTracking()
            .Where(m => m.IntegracaoId == integracao.Id && (m.FluxoIntegracaoId == fluxo.Id || m.FluxoIntegracaoId == null))
            .OrderBy(m => m.Ordem).ToListAsync(ct);

        var body = ProcessadorMapeamento.MontarBodySaida(dados, itens, maps);
        await HttpEnviarAsync(integracao, fluxo.EndpointSaida!, fluxo.MetodoHttpSaida ?? "POST", body.ToJsonString(), fluxo.Id, ct);
    }

    private async Task<bool> ConsultarAsync(EtapaFluxoIntegracao etapa, Integracao integracao,
        Dictionary<string, object?> dados, Dictionary<string, JsonElement> contexto, CancellationToken ct)
    {
        var endpoint = Substituir(etapa.Endpoint, dados, contexto);

        if (etapa.CampoChaveConsulta is not null && etapa.CampoInternoChave is not null)
        {
            var valor = dados.GetValueOrDefault(etapa.CampoInternoChave)?.ToString();
            if (string.IsNullOrEmpty(valor)) return false;
            endpoint += (endpoint.Contains('?') ? "&" : "?") + $"{etapa.CampoChaveConsulta}={valor}";
        }

        using var client = Criar(integracao);
        var resp = await client.GetAsync(MontarUrl(integracao, endpoint), ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        await LogAsync(integracao.Id, etapa.FluxoIntegracaoId, endpoint, "GET", null, body, resp, ct);
        if (!resp.IsSuccessStatusCode) return false;

        if (etapa.ArmazenarResultadoComo is not null && !string.IsNullOrWhiteSpace(body))
        {
            var doc = JsonDocument.Parse(body);
            var el = doc.RootElement;
            if (etapa.CampoResultadoResponse is not null && el.TryGetProperty(etapa.CampoResultadoResponse, out var c)) el = c;
            if (el.ValueKind == JsonValueKind.Array) el = el.GetArrayLength() > 0 ? el[0] : default;
            if (el.ValueKind != JsonValueKind.Undefined) contexto[etapa.ArmazenarResultadoComo] = el;
        }

        return true;
    }

    private async Task<bool> EnviarAsync(EtapaFluxoIntegracao etapa, Integracao integracao,
        Dictionary<string, object?> dados, List<Dictionary<string, object?>>? itens,
        Dictionary<string, JsonElement> contexto, CancellationToken ct)
    {
        var endpoint = Substituir(etapa.Endpoint, dados, contexto);
        string bodyStr;

        if (etapa.TemplateBody is not null)
        {
            bodyStr = Substituir(etapa.TemplateBody, dados, contexto);
        }
        else
        {
            var maps = await db.MapeamentosIntegracao.AsNoTracking()
                .Where(m => m.IntegracaoId == integracao.Id && m.FluxoIntegracaoId == etapa.FluxoIntegracaoId)
                .OrderBy(m => m.Ordem).ToListAsync(ct);

            var dadosExt = new Dictionary<string, object?>(dados, StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in contexto) dadosExt[k] = v.ToString();

            bodyStr = ProcessadorMapeamento.MontarBodySaida(dadosExt, itens, maps).ToJsonString();
        }

        return await HttpEnviarAsync(integracao, endpoint, etapa.MetodoHttp, bodyStr, etapa.FluxoIntegracaoId, ct);
    }

    private async Task<bool> HttpEnviarAsync(Integracao integracao, string endpoint, string metodo, string body, long? fluxoId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var client = Criar(integracao);
        var url = MontarUrl(integracao, endpoint);
        var req = new HttpRequestMessage(new HttpMethod(metodo), url)
        { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

        var resp = await client.SendAsync(req, ct);
        var respBody = await resp.Content.ReadAsStringAsync(ct);
        sw.Stop();

        await LogAsync(integracao.Id, fluxoId, url, metodo, body, respBody, resp, ct, sw.ElapsedMilliseconds);
        return resp.IsSuccessStatusCode;
    }

    private static string MontarUrl(Integracao integracao, string endpoint)
        => endpoint.StartsWith("http") ? endpoint : $"{integracao.ApiUrl?.TrimEnd('/')}/{endpoint.TrimStart('/')}";

    private static HttpClient Criar(Integracao integracao)
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        switch (integracao.TipoAuth)
        {
            case TipoAutenticacao.Bearer:
                if (!string.IsNullOrEmpty(integracao.ApiToken))
                    c.DefaultRequestHeaders.Authorization = new("Bearer", integracao.ApiToken);
                break;
            case TipoAutenticacao.BasicAuth:
                if (!string.IsNullOrEmpty(integracao.AuthUsuario))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes($"{integracao.AuthUsuario}:{integracao.AuthSenha}");
                    c.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(bytes));
                }
                break;
            case TipoAutenticacao.ApiKey:
                if (!string.IsNullOrEmpty(integracao.ApiKeyHeader) && !string.IsNullOrEmpty(integracao.ApiKeyValor))
                    c.DefaultRequestHeaders.TryAddWithoutValidation(integracao.ApiKeyHeader, integracao.ApiKeyValor);
                break;
        }

        if (!string.IsNullOrWhiteSpace(integracao.HeadersFixos))
        {
            try
            {
                var headers = JsonDocument.Parse(integracao.HeadersFixos);
                foreach (var h in headers.RootElement.EnumerateObject())
                    c.DefaultRequestHeaders.TryAddWithoutValidation(h.Name, h.Value.GetString());
            }
            catch { }
        }

        return c;
    }

    private static bool AvaliarCondicao(EtapaFluxoIntegracao etapa, Dictionary<string, JsonElement> ctx)
    {
        if (etapa.CondicaoCampo is null || etapa.CondicaoOperador is null) return true;

        string? val = null;
        var partes = etapa.CondicaoCampo.Split('.', 2);
        if (ctx.TryGetValue(partes[0], out var el))
        {
            if (partes.Length > 1 && el.ValueKind == JsonValueKind.Object && el.TryGetProperty(partes[1], out var p))
                val = p.ToString();
            else if (el.ValueKind != JsonValueKind.Undefined)
                val = el.ToString();
        }

        return etapa.CondicaoOperador switch
        {
            OperadorFiltro.Igual => string.Equals(val, etapa.CondicaoValor, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Diferente => !string.Equals(val, etapa.CondicaoValor, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Vazio => string.IsNullOrWhiteSpace(val),
            OperadorFiltro.NaoVazio => !string.IsNullOrWhiteSpace(val),
            _ => true
        };
    }

    private static string Substituir(string template, Dictionary<string, object?> dados, Dictionary<string, JsonElement> ctx)
    {
        return PlaceholderRegex().Replace(template, m =>
        {
            var chave = m.Groups[1].Value;
            if (dados.TryGetValue(chave, out var v) && v is not null) return v.ToString() ?? "";
            var p = chave.Split('.', 2);
            if (ctx.TryGetValue(p[0], out var el))
            {
                if (p.Length > 1 && el.ValueKind == JsonValueKind.Object && el.TryGetProperty(p[1], out var prop))
                    return prop.ToString();
                return el.ToString();
            }
            return "";
        });
    }

    [GeneratedRegex(@"\{\{(.+?)\}\}")]
    private static partial Regex PlaceholderRegex();

    private async Task LogAsync(long integId, long? fluxoId, string? ep, string? met, string? req, string? resp,
        HttpResponseMessage r, CancellationToken ct, long? ms = null)
    {
        db.LogsIntegracao.Add(new LogIntegracao
        {
            EmpresaId = db.EmpresaIdAtual, IntegracaoId = integId, FluxoIntegracaoId = fluxoId,
            Direcao = DirecaoFluxo.Saida, Sucesso = r.IsSuccessStatusCode, Endpoint = ep, MetodoHttp = met,
            StatusCode = (int)r.StatusCode, RequestBody = req, ResponseBody = resp,
            Erro = r.IsSuccessStatusCode ? null : $"Status {r.StatusCode}", DuracaoMs = (int)(ms ?? 0)
        });
        await db.SaveChangesAsync(ct);
    }
}

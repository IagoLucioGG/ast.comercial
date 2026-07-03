using System.Text.Json;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using AST.Comercial.Infrastructure.Integracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

public class IntegracoesController(IIntegracaoServico servico, AppDbContext db, ILogger<IntegracoesController> logger) : ODataController
{
    // --- Integracoes CRUD ---

    [EnableQuery]
    public IQueryable<Integracao> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Integracao>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Integracao> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Integracao> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    // --- Mapeamentos ---

    [EnableQuery]
    [HttpGet("odata/Integracoes@Mapeamentos")]
    public IQueryable<MapeamentoCampoIntegracao> ObterMapeamentos() => servico.ObterMapeamentos();

    [HttpPost("odata/Integracoes@Mapeamentos")]
    public async Task<ActionResult> CriarMapeamento([FromBody] Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarMapeamentoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Integracoes@Mapeamentos({key})")]
    public async Task<ActionResult> AtualizarMapeamento(long key, [FromBody] Delta<MapeamentoCampoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarMapeamentoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Integracoes@Mapeamentos({key})")]
    public async Task<ActionResult> RemoverMapeamento(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverMapeamentoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    // --- Fluxos ---

    [EnableQuery]
    [HttpGet("odata/Integracoes@Fluxos")]
    public IQueryable<FluxoIntegracao> ObterFluxos() => servico.ObterFluxos();

    [HttpPost("odata/Integracoes@Fluxos")]
    public async Task<ActionResult> CriarFluxo([FromBody] Delta<FluxoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarFluxoAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Integracoes@Fluxos({key})")]
    public async Task<ActionResult> AtualizarFluxo(long key, [FromBody] Delta<FluxoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarFluxoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Integracoes@Fluxos({key})")]
    public async Task<ActionResult> RemoverFluxo(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverFluxoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    // --- Etapas ---

    [EnableQuery]
    [HttpGet("odata/Integracoes@Etapas")]
    public IQueryable<EtapaFluxoIntegracao> ObterEtapas() => servico.ObterEtapas();

    [HttpPost("odata/Integracoes@Etapas")]
    public async Task<ActionResult> CriarEtapa([FromBody] Delta<EtapaFluxoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarEtapaAsync(delta, ct);
        return Created(resultado);
    }

    [HttpPatch("odata/Integracoes@Etapas({key})")]
    public async Task<ActionResult> AtualizarEtapa(long key, [FromBody] Delta<EtapaFluxoIntegracao> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarEtapaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpDelete("odata/Integracoes@Etapas({key})")]
    public async Task<ActionResult> RemoverEtapa(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverEtapaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    // --- Logs ---

    [EnableQuery]
    [HttpGet("odata/Integracoes@Logs")]
    public IQueryable<LogIntegracao> ObterLogs() => servico.ObterLogs();

    // --- Webhook Callback ---

    [AllowAnonymous]
    [HttpPost("integracoes/callback/{chave}")]
    public async Task<ActionResult> Callback(string chave, CancellationToken ct)
    {
        var integracao = await servico.ObterPorChaveWebhookAsync(chave, ct);
        if (integracao is null) return NotFound();

        JsonElement payload;
        try
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            payload = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return BadRequest(new { erro = "Payload JSON inválido." });
        }

        var fluxos = await db.FluxosIntegracao.AsNoTracking()
            .Where(f => f.IntegracaoId == integracao.Id
                && f.Direcao == DirecaoFluxo.Entrada
                && f.ModoEntrada == ModoEntrada.Webhook
                && f.Ativo)
            .ToListAsync(ct);

        var fluxosAplicaveis = fluxos.Where(f =>
        {
            if (string.IsNullOrEmpty(f.WebhookCampoIdentificador)) return true;
            var valor = ObterValorPayload(payload, f.WebhookCampoIdentificador);
            return string.Equals(valor, f.WebhookValorIdentificador, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        if (fluxosAplicaveis.Count == 0)
        {
            logger.LogWarning("Webhook recebido para integração {IntegracaoId} mas nenhum fluxo aplicável encontrado", integracao.Id);
            return Ok(new { recebido = true, processado = false });
        }

        foreach (var fluxo in fluxosAplicaveis)
        {
            try
            {
                await ProcessarWebhookFluxoAsync(integracao, fluxo, payload, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar webhook para fluxo {FluxoId}", fluxo.Id);
                db.LogsIntegracao.Add(new LogIntegracao
                {
                    EmpresaId = integracao.EmpresaId,
                    IntegracaoId = integracao.Id,
                    FluxoIntegracaoId = fluxo.Id,
                    Direcao = DirecaoFluxo.Entrada,
                    Sucesso = false,
                    RequestBody = payload.GetRawText(),
                    Erro = ex.Message
                });
                await db.SaveChangesAsync(ct);
            }
        }

        return Ok(new { recebido = true, processado = true });
    }

    private async Task ProcessarWebhookFluxoAsync(Integracao integracao, FluxoIntegracao fluxo, JsonElement payload, CancellationToken ct)
    {
        var mapeamentos = await db.MapeamentosIntegracao.AsNoTracking()
            .Where(m => m.IntegracaoId == integracao.Id
                && (m.FluxoIntegracaoId == fluxo.Id || m.FluxoIntegracaoId == null)
                && (m.Direcao == DirecaoMapeamento.Entrada || m.Direcao == DirecaoMapeamento.Ambos))
            .OrderBy(m => m.Ordem)
            .ToListAsync(ct);

        var dados = ProcessadorMapeamento.ExtrairDadosEntrada(payload, mapeamentos);

        long? registroExistenteId = null;
        if (!string.IsNullOrEmpty(fluxo.CampoChaveDuplicidade) && dados.TryGetValue(fluxo.CampoChaveDuplicidade, out var chaveValor) && chaveValor is not null)
        {
            registroExistenteId = await BuscarRegistroExistenteAsync(fluxo.EntidadeAlvo, fluxo.CampoChaveDuplicidade, chaveValor.ToString()!, integracao.EmpresaId, ct);
        }

        var acao = fluxo.Duplicidade switch
        {
            ComportamentoDuplicidade.Criar when registroExistenteId is not null => "ignorado",
            ComportamentoDuplicidade.Atualizar when registroExistenteId is null => "ignorado",
            ComportamentoDuplicidade.Ignorar when registroExistenteId is not null => "ignorado",
            _ => registroExistenteId is not null ? "atualizado" : "criado"
        };

        long? registroId = null;
        if (acao != "ignorado")
        {
            registroId = await SalvarEntidadeAsync(fluxo.EntidadeAlvo, dados, registroExistenteId, integracao.EmpresaId, ct);
        }

        db.LogsIntegracao.Add(new LogIntegracao
        {
            EmpresaId = integracao.EmpresaId,
            IntegracaoId = integracao.Id,
            FluxoIntegracaoId = fluxo.Id,
            Direcao = DirecaoFluxo.Entrada,
            Sucesso = true,
            RequestBody = payload.GetRawText(),
            RegistroId = registroId ?? registroExistenteId,
            EntidadeAlvo = fluxo.EntidadeAlvo
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<long?> BuscarRegistroExistenteAsync(EntidadeAlvo entidade, string campo, string valor, long empresaId, CancellationToken ct)
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

    private async Task<long?> SalvarEntidadeAsync(EntidadeAlvo entidade, Dictionary<string, object?> dados, long? existenteId, long empresaId, CancellationToken ct)
    {
        return entidade switch
        {
            EntidadeAlvo.Cliente => await SalvarClienteAsync(dados, existenteId, empresaId, ct),
            EntidadeAlvo.Negocio => await SalvarNegocioAsync(dados, existenteId, empresaId, ct),
            EntidadeAlvo.Produto => await SalvarProdutoAsync(dados, existenteId, empresaId, ct),
            _ => null
        };
    }

    private async Task<long> SalvarClienteAsync(Dictionary<string, object?> dados, long? existenteId, long empresaId, CancellationToken ct)
    {
        Cliente cliente;
        if (existenteId is not null)
        {
            cliente = await db.Clientes.FirstAsync(c => c.Id == existenteId.Value, ct);
        }
        else
        {
            cliente = new Cliente { EmpresaId = empresaId };
            db.Clientes.Add(cliente);
        }
        AplicarDados(cliente, dados);
        await db.SaveChangesAsync(ct);
        return cliente.Id;
    }

    private async Task<long> SalvarNegocioAsync(Dictionary<string, object?> dados, long? existenteId, long empresaId, CancellationToken ct)
    {
        Negocio negocio;
        if (existenteId is not null)
        {
            negocio = await db.Negocios.FirstAsync(n => n.Id == existenteId.Value, ct);
        }
        else
        {
            negocio = new Negocio { EmpresaId = empresaId };
            db.Negocios.Add(negocio);
        }
        AplicarDados(negocio, dados);
        await db.SaveChangesAsync(ct);
        return negocio.Id;
    }

    private async Task<long> SalvarProdutoAsync(Dictionary<string, object?> dados, long? existenteId, long empresaId, CancellationToken ct)
    {
        Produto produto;
        if (existenteId is not null)
        {
            produto = await db.Produtos.FirstAsync(p => p.Id == existenteId.Value, ct);
        }
        else
        {
            produto = new Produto { EmpresaId = empresaId };
            db.Produtos.Add(produto);
        }
        AplicarDados(produto, dados);
        await db.SaveChangesAsync(ct);
        return produto.Id;
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

    private static string? ObterValorPayload(JsonElement payload, string campo)
    {
        var partes = campo.Split('.');
        var atual = payload;
        foreach (var parte in partes)
        {
            if (atual.ValueKind != JsonValueKind.Object) return null;
            if (!atual.TryGetProperty(parte, out var proximo)) return null;
            atual = proximo;
        }
        return atual.ValueKind == JsonValueKind.String ? atual.GetString() : atual.GetRawText();
    }
}

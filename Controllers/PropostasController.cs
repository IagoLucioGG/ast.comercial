using System.Text.Json;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class PropostasController(IPropostaServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Proposta> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Proposta>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Post([FromBody] Delta<Proposta> delta, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(delta, ct);
        return Created(resultado);
    }

    public async Task<ActionResult> Patch(long key, [FromBody] Delta<Proposta> delta, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(key, delta, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    public async Task<ActionResult> Delete(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    /// <summary>Gera a chave (se faltar), marca como compartilhada e devolve { chave, url }.</summary>
    [HttpPost("odata/Propostas({key})/Compartilhar")]
    public async Task<ActionResult> Compartilhar(long key, CancellationToken ct)
    {
        var resultado = await servico.CompartilharAsync(key, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>Recalcula totais dos itens e o valor da proposta (itens − desconto + frete).</summary>
    [HttpPost("odata/Propostas({key})/Recalcular")]
    public async Task<ActionResult> Recalcular(long key, CancellationToken ct)
    {
        var resultado = await servico.RecalcularAsync(key, ct);
        return resultado is null ? NotFound() : Entidade(resultado);
    }

    [HttpPost("odata/Propostas({key})/Aprovar")]
    public async Task<ActionResult> Aprovar(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.AprovarAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("odata/Propostas({key})/Rejeitar")]
    public async Task<ActionResult> Rejeitar(long key, CancellationToken cancellationToken)
    {
        try
        {
            await servico.RejeitarAsync(key, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Produtos")]
    public IQueryable<ItemProposta> ObterProdutos() => servico.ObterProdutos();

    [HttpPost("odata/Propostas@Produtos")]
    public async Task<ActionResult> CriarProduto([FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<ItemProposta>(corpo);
        var resultado = await servico.CriarProdutoAsync(delta, ct);
        return Entidade(resultado, 201);
    }

    [HttpPatch("odata/Propostas@Produtos({key})")]
    public async Task<ActionResult> AtualizarProduto(long key, [FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<ItemProposta>(corpo);
        var resultado = await servico.AtualizarProdutoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Entidade(resultado);
    }

    [HttpDelete("odata/Propostas@Produtos({key})")]
    public async Task<ActionResult> RemoverProduto(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverProdutoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Secoes")]
    public IQueryable<SecaoProposta> ObterSecoes() => servico.ObterSecoes();

    [HttpPost("odata/Propostas@Secoes")]
    public async Task<ActionResult> CriarSecao([FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<SecaoProposta>(corpo);
        var resultado = await servico.CriarSecaoAsync(delta, ct);
        return Entidade(resultado, 201);
    }

    [HttpPatch("odata/Propostas@Secoes({key})")]
    public async Task<ActionResult> AtualizarSecao(long key, [FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<SecaoProposta>(corpo);
        var resultado = await servico.AtualizarSecaoAsync(key, delta, ct);
        return resultado is null ? NotFound() : Entidade(resultado);
    }

    [HttpDelete("odata/Propostas@Secoes({key})")]
    public async Task<ActionResult> RemoverSecao(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverSecaoAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [EnableQuery]
    [HttpGet("odata/Propostas@Parcelas")]
    public IQueryable<ParcelaProposta> ObterParcelas() => servico.ObterParcelas();

    [HttpPost("odata/Propostas@Parcelas")]
    public async Task<ActionResult> CriarParcela([FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<ParcelaProposta>(corpo);
        var resultado = await servico.CriarParcelaAsync(delta, ct);
        return Entidade(resultado, 201);
    }

    [HttpPatch("odata/Propostas@Parcelas({key})")]
    public async Task<ActionResult> AtualizarParcela(long key, [FromBody] JsonElement corpo, CancellationToken ct)
    {
        var delta = DeltaDeJson<ParcelaProposta>(corpo);
        var resultado = await servico.AtualizarParcelaAsync(key, delta, ct);
        return resultado is null ? NotFound() : Entidade(resultado);
    }

    [HttpDelete("odata/Propostas@Parcelas({key})")]
    public async Task<ActionResult> RemoverParcela(long key, CancellationToken ct)
    {
        var removido = await servico.RemoverParcelaAsync(key, ct);
        return removido ? NoContent() : NotFound();
    }

    [HttpGet("odata/Propostas@StatusAprovacao")]
    public ActionResult ObterStatusAprovacao()
    {
        var valores = Enum.GetNames<StatusAprovacaoProposta>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    /// <summary>
    /// As rotas "odata/Propostas@..." não são rotas OData válidas (o roteador as registra como MVC comum),
    /// então Delta&lt;T&gt; chegava vazio (ex.: PropostaId = 0 → violação de FK). Monta o Delta a partir do JSON,
    /// aceitando apenas propriedades escalares (sem navegação/coleções) e nunca Id/EmpresaId/datas de controle.
    /// </summary>
    private static Delta<T> DeltaDeJson<T>(JsonElement corpo) where T : class
    {
        if (corpo.ValueKind != JsonValueKind.Object)
            throw new AST.Comercial.Application.RegraNegocioException("Corpo da requisição deve ser um objeto JSON.");

        var delta = new Delta<T>();
        foreach (var prop in corpo.EnumerateObject())
        {
            if (prop.Name.StartsWith('@')) continue; // anotações OData
            var info = typeof(T).GetProperty(prop.Name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (info is null || !info.CanWrite || CamposBloqueados.Contains(info.Name)) continue;

            var tipo = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
            var escalar = tipo.IsPrimitive || tipo.IsEnum || tipo == typeof(string) || tipo == typeof(decimal)
                || tipo == typeof(DateTime) || tipo == typeof(DateTimeOffset) || tipo == typeof(Guid);
            if (!escalar) continue;

            object? valor;
            try
            {
                valor = prop.Value.ValueKind == JsonValueKind.Null
                    ? null
                    : JsonSerializer.Deserialize(prop.Value.GetRawText(), info.PropertyType, OpcoesJson);
            }
            catch (JsonException)
            {
                throw new AST.Comercial.Application.RegraNegocioException($"Valor inválido para '{info.Name}'.");
            }

            if (valor is DateTime dt && dt.Kind != DateTimeKind.Utc)
                valor = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
            if (valor is null && info.PropertyType.IsValueType && Nullable.GetUnderlyingType(info.PropertyType) is null)
                throw new AST.Comercial.Application.RegraNegocioException($"'{info.Name}' não aceita nulo.");

            delta.TrySetPropertyValue(info.Name, valor);
        }
        return delta;
    }

    /// <summary>
    /// Rotas fora do roteamento OData serializam em camelCase; as entidades são devolvidas em PascalCase,
    /// igual às respostas OData (o front usa Id, PropostaId, Valor...).
    /// </summary>
    private static JsonResult Entidade(object valor, int status = 200)
        => new(valor, OpcoesSaida) { StatusCode = status };

    private static readonly JsonSerializerOptions OpcoesSaida = new()
    {
        PropertyNamingPolicy = null,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly HashSet<string> CamposBloqueados = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "EmpresaId", "CriadoEm", "AtualizadoEm", "Ativo"
    };

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}

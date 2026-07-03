using System.Text.Json;
using System.Text.Json.Nodes;
using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Infrastructure.Integracoes;

/// <summary>
/// Engine que monta/desmonta JSON usando mapeamentos de campos.
/// Suporta dot notation (objeto.campo), arrays ([*]) e campos personalizados.
/// </summary>
public static class ProcessadorMapeamento
{
    /// <summary>
    /// Monta body JSON de saída a partir dos dados internos + mapeamentos.
    /// </summary>
    public static JsonObject MontarBodySaida(
        Dictionary<string, object?> dadosRegistro,
        List<Dictionary<string, object?>>? dadosItens,
        IEnumerable<MapeamentoCampoIntegracao> mapeamentos)
    {
        var body = new JsonObject();
        var mapeamentosSimples = mapeamentos.Where(m => !m.CampoExterno.Contains("[*]")).OrderBy(m => m.Ordem);
        var mapeamentosArray = mapeamentos.Where(m => m.CampoExterno.Contains("[*]")).OrderBy(m => m.Ordem);

        foreach (var map in mapeamentosSimples)
        {
            var valor = ResolverValorInterno(map.CampoInterno, dadosRegistro);
            valor ??= map.ValorPadrao;
            if (valor is null && map.Obrigatorio) continue;
            valor = AplicarTransformacao(valor, map.Transformacao);
            DefinirValorAninhado(body, map.CampoExterno, valor);
        }

        if (dadosItens is { Count: > 0 })
        {
            var gruposArray = mapeamentosArray.GroupBy(m => ExtrairPathArray(m.CampoExterno));

            foreach (var grupo in gruposArray)
            {
                var arrayNode = new JsonArray();
                foreach (var item in dadosItens)
                {
                    var itemNode = new JsonObject();
                    foreach (var map in grupo)
                    {
                        var campo = ExtrairCampoAposArray(map.CampoExterno);
                        var valor = ResolverValorInterno(map.CampoInterno, item);
                        valor ??= map.ValorPadrao;
                        valor = AplicarTransformacao(valor, map.Transformacao);
                        if (campo.Contains('.'))
                            DefinirValorAninhado(itemNode, campo, valor);
                        else if (valor is not null)
                            itemNode[campo] = JsonValue.Create(valor);
                    }
                    arrayNode.Add(itemNode);
                }
                DefinirArrayAninhado(body, grupo.Key, arrayNode);
            }
        }

        return body;
    }

    /// <summary>
    /// Extrai dados de entrada do JSON externo usando mapeamentos.
    /// </summary>
    public static Dictionary<string, object?> ExtrairDadosEntrada(
        JsonElement payload,
        IEnumerable<MapeamentoCampoIntegracao> mapeamentos)
    {
        var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in mapeamentos.Where(m => !m.CampoExterno.Contains("[*]")).OrderBy(m => m.Ordem))
        {
            var valor = ResolverValorJson(payload, map.CampoExterno);
            valor ??= map.ValorPadrao;
            valor = AplicarTransformacao(valor, map.Transformacao);
            dados[map.CampoInterno] = valor;
        }

        return dados;
    }

    /// <summary>
    /// Extrai itens de array no JSON de entrada.
    /// </summary>
    public static List<Dictionary<string, object?>> ExtrairItensEntrada(
        JsonElement payload,
        string campoArray,
        IEnumerable<MapeamentoCampoIntegracao> mapeamentos)
    {
        var resultado = new List<Dictionary<string, object?>>();
        var arrayElement = ResolverElementoJson(payload, campoArray);
        if (arrayElement is null || arrayElement.Value.ValueKind != JsonValueKind.Array) return resultado;

        var maps = mapeamentos.Where(m => m.CampoExterno.Contains("[*]")).ToList();

        foreach (var item in arrayElement.Value.EnumerateArray())
        {
            var dados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var map in maps)
            {
                var campo = ExtrairCampoAposArray(map.CampoExterno);
                var valor = ResolverValorJson(item, campo);
                valor ??= map.ValorPadrao;
                valor = AplicarTransformacao(valor, map.Transformacao);
                dados[map.CampoInterno] = valor;
            }
            resultado.Add(dados);
        }

        return resultado;
    }

    private static object? ResolverValorInterno(string campo, Dictionary<string, object?> dados)
    {
        if (dados.TryGetValue(campo, out var valor)) return valor;

        var partes = campo.Split('.');
        if (partes.Length > 1 && dados.TryGetValue(partes[0], out var obj) && obj is Dictionary<string, object?> sub)
            return ResolverValorInterno(string.Join('.', partes[1..]), sub);

        return null;
    }

    private static object? ResolverValorJson(JsonElement element, string path)
    {
        var partes = path.Split('.');
        var atual = element;

        foreach (var parte in partes)
        {
            if (atual.ValueKind != JsonValueKind.Object) return null;
            if (!atual.TryGetProperty(parte, out var proximo)) return null;
            atual = proximo;
        }

        return atual.ValueKind switch
        {
            JsonValueKind.String => atual.GetString(),
            JsonValueKind.Number => atual.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => atual.GetRawText()
        };
    }

    private static JsonElement? ResolverElementoJson(JsonElement element, string path)
    {
        var partes = path.Split('.');
        var atual = element;

        foreach (var parte in partes)
        {
            if (atual.ValueKind != JsonValueKind.Object) return null;
            if (!atual.TryGetProperty(parte, out var proximo)) return null;
            atual = proximo;
        }

        return atual;
    }

    private static void DefinirValorAninhado(JsonObject obj, string path, object? valor)
    {
        if (valor is null) return;
        var partes = path.Split('.');
        var atual = obj;

        for (var i = 0; i < partes.Length - 1; i++)
        {
            if (!atual.ContainsKey(partes[i]))
                atual[partes[i]] = new JsonObject();
            atual = atual[partes[i]]!.AsObject();
        }

        atual[partes[^1]] = JsonValue.Create(valor);
    }

    private static void DefinirArrayAninhado(JsonObject obj, string path, JsonArray array)
    {
        var partes = path.Split('.');
        var atual = obj;

        for (var i = 0; i < partes.Length - 1; i++)
        {
            if (!atual.ContainsKey(partes[i]))
                atual[partes[i]] = new JsonObject();
            atual = atual[partes[i]]!.AsObject();
        }

        atual[partes[^1]] = array;
    }

    private static string ExtrairPathArray(string campo)
    {
        var idx = campo.IndexOf("[*]");
        return idx > 0 ? campo[..idx] : campo;
    }

    private static string ExtrairCampoAposArray(string campo)
    {
        var idx = campo.IndexOf("[*].");
        return idx >= 0 ? campo[(idx + 4)..] : campo;
    }

    private static object? AplicarTransformacao(object? valor, string? transformacao)
    {
        if (valor is null || string.IsNullOrWhiteSpace(transformacao)) return valor;
        var str = valor.ToString() ?? "";

        return transformacao.ToLowerInvariant() switch
        {
            "uppercase" => str.ToUpperInvariant(),
            "lowercase" => str.ToLowerInvariant(),
            "trim" => str.Trim(),
            _ when transformacao.StartsWith("date:") => DateTime.TryParse(str, out var d) ? d.ToString(transformacao[5..]) : str,
            _ when transformacao.StartsWith("prefix:") => transformacao[7..] + str,
            _ when transformacao.StartsWith("suffix:") => str + transformacao[7..],
            _ => valor
        };
    }
}

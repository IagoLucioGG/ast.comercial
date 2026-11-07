using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Automacoes;

/// <summary>
/// Motor de execução de automações.
/// Processa um item da fila: define o tenant do item, carrega a automação, avalia filtros, executa ações, registra log.
/// O que o motor realmente suporta está descrito em <see cref="CatalogoAutomacao"/>.
/// </summary>
public class MotorAutomacao(AppDbContext db, ILogger<MotorAutomacao> logger) : IMotorAutomacao
{
    public async Task ProcessarItemFilaAsync(long filaAutomacaoId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // FilaAutomacao não é EntidadeBase (sem filtro de tenant): carrega sem includes para descobrir a empresa
        var fila = await db.FilaAutomacao.FirstOrDefaultAsync(f => f.Id == filaAutomacaoId, cancellationToken);
        if (fila is null) return;

        // TENANT: o escopo dos serviços de fundo nasce com EmpresaIdAtual = 0. Define a empresa do item
        // para que os filtros globais (EmpresaId + Ativo) enxerguem apenas os dados dessa empresa.
        db.EmpresaIdAtual = fila.EmpresaId;
        db.UsuarioAtual = string.IsNullOrWhiteSpace(fila.DisparadoPor) ? "Automação" : $"Automação ({fila.DisparadoPor})";

        var automacao = await db.Automacoes
            .Include(a => a.Filtros)
            .Include(a => a.Acoes.OrderBy(ac => ac.Ordem))
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == fila.AutomacaoId && a.EmpresaId == fila.EmpresaId, cancellationToken);

        if (automacao is null)
        {
            // Excluída, desativada ou de outra empresa: não executa
            fila.Status = StatusFila.Cancelado;
            fila.FinalizadoEm = DateTime.UtcNow;
            fila.UltimoErro = "Automação inexistente, desativada ou excluída.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var execucao = new ExecucaoAutomacao
        {
            EmpresaId = fila.EmpresaId,
            AutomacaoId = automacao.Id,
            RegistroId = fila.RegistroId,
            EntidadeAlvo = fila.EntidadeAlvo,
            DisparadoPor = fila.DisparadoPor
        };

        try
        {
            var dados = await MontarDadosAsync(fila, cancellationToken);
            if (!AvaliarFiltros(automacao.Filtros.Where(f => f.Ativo).ToList(), dados))
            {
                execucao.Status = StatusExecucao.FalhaFiltro;
                execucao.Detalhes = "{\"motivo\": \"Filtros não satisfeitos\"}";
                return;
            }

            var detalhes = new List<string>();
            foreach (var acao in automacao.Acoes.Where(a => a.Ativo).OrderBy(a => a.Ordem))
            {
                var resultado = await ExecutarAcaoAsync(acao, fila, cancellationToken);
                detalhes.Add($"{acao.Tipo}: {resultado}");
            }

            execucao.Status = StatusExecucao.Sucesso;
            execucao.Detalhes = JsonSerializer.Serialize(detalhes);
        }
        catch (Exception ex)
        {
            execucao.Status = StatusExecucao.Falha;
            execucao.Erro = ex.Message[..Math.Min(ex.Message.Length, 2000)];

            // Descarta alterações pendentes da ação que falhou para o log da execução conseguir ser salvo.
            // (O status da fila é gravado pelo processador, em outro DbContext.)
            db.ChangeTracker.Clear();

            throw;
        }
        finally
        {
            sw.Stop();
            execucao.DuracaoMs = (int)sw.ElapsedMilliseconds;
            execucao.FinalizadaEm = DateTime.UtcNow;
            db.ExecucoesAutomacao.Add(execucao);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // ---------------- Dados do registro ----------------

    /// <summary>
    /// Os publicadores de evento não enviam DadosEvento, então o motor monta o snapshot atual do registro
    /// (campos escalares) para os filtros. DadosEvento, quando existir, sobrescreve as chaves coincidentes.
    /// </summary>
    private async Task<Dictionary<string, string?>> MontarDadosAsync(FilaAutomacao fila, CancellationToken ct)
    {
        var dados = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (fila.RegistroId is long id)
        {
            var entidade = await CarregarParaLeituraAsync(fila.EntidadeAlvo, id, fila.EmpresaId, ct);
            if (entidade is not null)
            {
                foreach (var campo in CatalogoAutomacao.Campos(fila.EntidadeAlvo))
                {
                    var valor = entidade.GetType().GetProperty(campo.Nome)?.GetValue(entidade);
                    dados[campo.Nome] = FormatarValor(valor);
                }
            }
        }

        foreach (var (k, v) in ParseDadosEvento(fila.DadosEvento))
            dados[k] = v;

        return dados;
    }

    // IgnoreQueryFilters para o gatilho Removido (registro já inativo), sempre com filtro explícito de empresa
    private async Task<object?> CarregarParaLeituraAsync(EntidadeAlvo tipo, long id, long empresaId, CancellationToken ct) => tipo switch
    {
        EntidadeAlvo.Cliente => await db.Clientes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        EntidadeAlvo.PessoaContato => await db.PessoasContato.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        EntidadeAlvo.Negocio => await db.Negocios.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        EntidadeAlvo.Atividade => await db.Atividades.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        EntidadeAlvo.Proposta => await db.Propostas.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        EntidadeAlvo.Funil => await db.Funis.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId, ct),
        _ => null
    };

    private static string? FormatarValor(object? valor) => valor switch
    {
        null => null,
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString()
    };

    // ---------------- Filtros ----------------

    private static bool AvaliarFiltros(IReadOnlyCollection<FiltroAutomacao> filtros, Dictionary<string, string?> dados)
    {
        if (filtros.Count == 0) return true;

        // Mesmo Grupo = AND; grupos diferentes = OR
        foreach (var grupo in filtros.GroupBy(f => f.Grupo))
        {
            if (grupo.OrderBy(f => f.Ordem).All(f => AvaliarFiltro(f, dados)))
                return true;
        }
        return false;
    }

    private static Dictionary<string, string?> ParseDadosEvento(string? dadosEvento)
    {
        var resultado = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(dadosEvento)) return resultado;

        try
        {
            using var doc = JsonDocument.Parse(dadosEvento);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return resultado;
            foreach (var prop in doc.RootElement.EnumerateObject())
                resultado[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.ToString();
        }
        catch (JsonException) { }

        return resultado;
    }

    private static bool AvaliarFiltro(FiltroAutomacao filtro, Dictionary<string, string?> dados)
    {
        dados.TryGetValue(filtro.CampoReferencia, out var valorCampo);
        var valorFiltro = filtro.Valor;

        return filtro.Operador switch
        {
            OperadorFiltro.Igual => string.Equals(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Diferente => !string.Equals(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Contem => valorCampo?.Contains(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            OperadorFiltro.NaoContem => valorCampo?.Contains(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) != true,
            OperadorFiltro.Maior => valorCampo is not null && CompararNumerico(valorCampo, valorFiltro) > 0,
            OperadorFiltro.Menor => valorCampo is not null && CompararNumerico(valorCampo, valorFiltro) < 0,
            OperadorFiltro.MaiorOuIgual => valorCampo is not null && CompararNumerico(valorCampo, valorFiltro) >= 0,
            OperadorFiltro.MenorOuIgual => valorCampo is not null && CompararNumerico(valorCampo, valorFiltro) <= 0,
            OperadorFiltro.Vazio => string.IsNullOrWhiteSpace(valorCampo),
            OperadorFiltro.NaoVazio => !string.IsNullOrWhiteSpace(valorCampo),
            OperadorFiltro.ComecaCom => valorCampo?.StartsWith(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            OperadorFiltro.TerminaCom => valorCampo?.EndsWith(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            // MudouPara/MudouDe não são suportados (o evento não traz o valor anterior): nunca satisfeitos
            _ => false
        };
    }

    private static int CompararNumerico(string? valorCampo, string? valorFiltro)
    {
        if (decimal.TryParse(valorCampo, NumberStyles.Number, CultureInfo.InvariantCulture, out var numCampo)
            && decimal.TryParse(valorFiltro, NumberStyles.Number, CultureInfo.InvariantCulture, out var numFiltro))
            return numCampo.CompareTo(numFiltro);

        // Datas ISO (comparação cronológica) ou, no limite, texto
        if (DateTime.TryParse(valorCampo, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtCampo)
            && DateTime.TryParse(valorFiltro, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtFiltro))
            return dtCampo.CompareTo(dtFiltro);

        return string.Compare(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------- Ações ----------------

    private async Task<string> ExecutarAcaoAsync(AcaoAutomacao acao, FilaAutomacao fila, CancellationToken ct)
    {
        logger.LogDebug("Executando {Tipo} automação {Id}", acao.Tipo, acao.AutomacaoId);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(acao.Configuracao) ? "{}" : acao.Configuracao);
        var config = doc.RootElement;

        switch (acao.Tipo)
        {
            case TipoAcao.EditarDados: return await ExecutarEditarDadosAsync(config, fila, ct);
            case TipoAcao.CriarTarefa: return await ExecutarCriarTarefaAsync(config, fila, ct);
            case TipoAcao.AlterarStatus: return await ExecutarAlterarStatusAsync(config, fila, ct);
            case TipoAcao.MoverEtapa: return await ExecutarMoverEtapaAsync(config, fila, ct);
            case TipoAcao.ConcluirTarefa: return await ExecutarConcluirTarefaAsync(fila, ct);
            case TipoAcao.AdicionarEtiqueta: return await ExecutarAdicionarEtiquetaAsync(config, fila, ct);
            case TipoAcao.RemoverEtiqueta: return await ExecutarRemoverEtiquetaAsync(config, fila, ct);
            case TipoAcao.CriarRegistroInteracao: return await ExecutarCriarRegistroInteracaoAsync(config, fila, ct);
            case TipoAcao.ExecutarWebhook: return await ExecutarWebhookAsync(config, ct);
            default:
                // EnviarEmail, NotificarUsuario, CriarContato, CriarProposta, ... não implementadas
                logger.LogWarning("Ação {Tipo} não implementada; ignorada (automação {Id})", acao.Tipo, acao.AutomacaoId);
                return "ignorada (não implementada)";
        }
    }

    private async Task<string> ExecutarEditarDadosAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return "ignorada (sem registro)";
        if (!config.TryGetProperty("alteracoes", out var alteracoes) || alteracoes.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Configuração inválida: 'alteracoes' ausente.");

        var entidade = await BuscarEntidadeAsync(fila.EntidadeAlvo, fila.RegistroId.Value, ct);
        if (entidade is null) return "ignorada (registro não encontrado)";

        var tipo = entidade.GetType();
        var alterados = 0;
        foreach (var alteracao in alteracoes.EnumerateArray())
        {
            var campo = alteracao.GetProperty("campo").GetString();
            if (string.IsNullOrWhiteSpace(campo)) continue;

            // Apenas campos escalares graváveis e nunca os protegidos (Id, EmpresaId, Ativo, ...)
            var prop = CatalogoAutomacao.PropriedadeEditavel(tipo, campo)
                ?? throw new InvalidOperationException($"O campo '{campo}' não pode ser alterado.");

            var valorJson = alteracao.GetProperty("valor");
            var texto = valorJson.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => valorJson.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => valorJson.GetRawText()
            };

            prop.SetValue(entidade, ConverterValor(texto, prop.PropertyType, campo));
            alterados++;
        }

        await db.SaveChangesAsync(ct);
        return $"OK ({alterados} campo(s))";
    }

    private async Task<string> ExecutarCriarTarefaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        var titulo = config.TryGetProperty("titulo", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        var prazo = config.TryGetProperty("prazo", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var dias) ? dias : 3;

        var atividade = new Atividade
        {
            EmpresaId = fila.EmpresaId,
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Tarefa automática" : titulo!,
            DataVencimento = DateTime.UtcNow.AddDays(Math.Clamp(prazo, 0, 3650)),
            ClienteId = fila.EntidadeAlvo == EntidadeAlvo.Cliente ? fila.RegistroId : null,
            NegocioId = fila.EntidadeAlvo == EntidadeAlvo.Negocio ? fila.RegistroId : null
        };

        if (config.TryGetProperty("tipo", out var tipoEl) && tipoEl.ValueKind == JsonValueKind.String
            && Enum.TryParse<TipoAtividade>(tipoEl.GetString(), true, out var tipoAtv) && Enum.IsDefined(tipoAtv))
            atividade.Tipo = tipoAtv;

        db.Atividades.Add(atividade);
        await db.SaveChangesAsync(ct);
        return $"OK (atividade {atividade.Id})";
    }

    private async Task<string> ExecutarAlterarStatusAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Negocio) return "ignorada (requer negócio)";
        if (!config.TryGetProperty("statusId", out var statusEl) || !statusEl.TryGetInt64(out var statusId))
            throw new InvalidOperationException("Configuração inválida: 'statusId' ausente.");

        if (!await db.StatusNegocios.AnyAsync(s => s.Id == statusId, ct))
            throw new InvalidOperationException($"Status {statusId} não existe nesta empresa.");

        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == fila.RegistroId, ct);
        if (negocio is null) return "ignorada (negócio não encontrado)";

        negocio.StatusId = statusId;
        await db.SaveChangesAsync(ct);
        return "OK";
    }

    private async Task<string> ExecutarMoverEtapaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Negocio) return "ignorada (requer negócio)";
        if (!config.TryGetProperty("etapaId", out var etapaEl) || !etapaEl.TryGetInt64(out var etapaId))
            throw new InvalidOperationException("Configuração inválida: 'etapaId' ausente.");

        var etapa = await db.EtapasFunil.AsNoTracking().FirstOrDefaultAsync(e => e.Id == etapaId, ct)
            ?? throw new InvalidOperationException($"Etapa {etapaId} não existe nesta empresa.");

        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == fila.RegistroId, ct);
        if (negocio is null) return "ignorada (negócio não encontrado)";

        negocio.EtapaId = etapa.Id;
        negocio.FunilId = etapa.FunilId;
        await db.SaveChangesAsync(ct);
        return "OK";
    }

    private async Task<string> ExecutarConcluirTarefaAsync(FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Atividade) return "ignorada (requer atividade)";

        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == fila.RegistroId, ct);
        if (atividade is null) return "ignorada (atividade não encontrada)";

        atividade.Concluida = true;
        atividade.ConcluidaEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return "OK";
    }

    private async Task<string> ExecutarAdicionarEtiquetaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return "ignorada (sem registro)";
        if (!config.TryGetProperty("etiquetaId", out var etEl) || !etEl.TryGetInt64(out var etiquetaId))
            throw new InvalidOperationException("Configuração inválida: 'etiquetaId' ausente.");

        if (!await db.Etiquetas.AnyAsync(e => e.Id == etiquetaId, ct))
            throw new InvalidOperationException($"Etiqueta {etiquetaId} não existe nesta empresa.");

        var jaTem = await db.EtiquetasRegistro.AnyAsync(e => e.EtiquetaId == etiquetaId
            && e.RegistroId == fila.RegistroId && e.EntidadeAlvo == fila.EntidadeAlvo, ct);
        if (jaTem) return "OK (já possuía)";

        db.EtiquetasRegistro.Add(new EtiquetaRegistro
        {
            EmpresaId = fila.EmpresaId,
            EtiquetaId = etiquetaId,
            RegistroId = fila.RegistroId.Value,
            EntidadeAlvo = fila.EntidadeAlvo
        });
        await db.SaveChangesAsync(ct);
        return "OK";
    }

    private async Task<string> ExecutarRemoverEtiquetaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return "ignorada (sem registro)";
        if (!config.TryGetProperty("etiquetaId", out var etEl) || !etEl.TryGetInt64(out var etiquetaId))
            throw new InvalidOperationException("Configuração inválida: 'etiquetaId' ausente.");

        // Sem IgnoreQueryFilters: o filtro global restringe à empresa do item e aos vínculos ativos
        var vinculos = await db.EtiquetasRegistro
            .Where(e => e.EtiquetaId == etiquetaId && e.RegistroId == fila.RegistroId && e.EntidadeAlvo == fila.EntidadeAlvo)
            .ToListAsync(ct);

        foreach (var v in vinculos) v.Ativo = false;
        if (vinculos.Count > 0) await db.SaveChangesAsync(ct);
        return vinculos.Count > 0 ? "OK" : "OK (não possuía)";
    }

    private async Task<string> ExecutarCriarRegistroInteracaoAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        var descricao = config.TryGetProperty("descricao", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;

        db.RegistrosAlteracao.Add(new RegistroAlteracao
        {
            Usuario = "Automação",
            Acao = "AutomacaoExecutada",
            EntidadeAlvo = fila.EntidadeAlvo,
            RegistroId = fila.RegistroId ?? 0,
            Titulo = string.IsNullOrWhiteSpace(descricao) ? "Registro de automação" : descricao,
            DadosDepois = fila.DadosEvento
        });
        await db.SaveChangesAsync(ct);
        return "OK";
    }

    private async Task<string> ExecutarWebhookAsync(JsonElement config, CancellationToken ct)
    {
        var url = config.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
        var erroUrl = CatalogoAutomacao.ValidarUrlWebhook(url);
        if (erroUrl is not null) throw new InvalidOperationException($"Webhook bloqueado: {erroUrl}");

        var metodo = config.TryGetProperty("metodo", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()!.ToUpperInvariant() : "POST";
        var corpo = config.TryGetProperty("corpo", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

        // O destino é validado novamente na conexão (anti-SSRF / DNS rebinding): só IPs públicos
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = async (ctx, token) =>
            {
                var ips = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, token);
                if (ips.Length == 0 || ips.Any(CatalogoAutomacao.EhEnderecoPrivado))
                    throw new HttpRequestException("Destino do webhook aponta para endereço local/privado.");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(ips, ctx.DnsEndPoint.Port, token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch { socket.Dispose(); throw; }
            }
        };

        using var httpClient = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };
        using var request = new HttpRequestMessage(new HttpMethod(metodo), url);

        if (config.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in headers.EnumerateObject())
                if (header.Value.ValueKind == JsonValueKind.String)
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
        }

        if (corpo is not null && metodo is not "GET" and not "DELETE")
            request.Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Webhook respondeu HTTP {(int)response.StatusCode}.");
        return $"OK (HTTP {(int)response.StatusCode})";
    }

    private async Task<EntidadeBase?> BuscarEntidadeAsync(EntidadeAlvo tipo, long id, CancellationToken ct) => tipo switch
    {
        EntidadeAlvo.Cliente => await db.Clientes.FirstOrDefaultAsync(e => e.Id == id, ct),
        EntidadeAlvo.PessoaContato => await db.PessoasContato.FirstOrDefaultAsync(e => e.Id == id, ct),
        EntidadeAlvo.Negocio => await db.Negocios.FirstOrDefaultAsync(e => e.Id == id, ct),
        EntidadeAlvo.Atividade => await db.Atividades.FirstOrDefaultAsync(e => e.Id == id, ct),
        EntidadeAlvo.Produto => await db.Produtos.FirstOrDefaultAsync(e => e.Id == id, ct),
        EntidadeAlvo.Proposta => await db.Propostas.FirstOrDefaultAsync(e => e.Id == id, ct),
        _ => null
    };

    private static object? ConverterValor(string? valor, Type tipo, string campo)
    {
        var tipoBase = Nullable.GetUnderlyingType(tipo) ?? tipo;
        var aceitaNulo = Nullable.GetUnderlyingType(tipo) is not null || !tipo.IsValueType;

        if (valor is null)
            return aceitaNulo ? null : throw new InvalidOperationException($"O campo '{campo}' não aceita valor nulo.");

        if (tipoBase == typeof(string)) return valor;
        if (tipoBase == typeof(int) && int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (tipoBase == typeof(long) && long.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
        if (tipoBase == typeof(decimal) && decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
        if (tipoBase == typeof(double) && double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var db2)) return db2;
        if (tipoBase == typeof(bool) && bool.TryParse(valor, out var b)) return b;
        if (tipoBase == typeof(DateTime) && DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)) return dt;
        if (tipoBase.IsEnum && Enum.TryParse(tipoBase, valor, true, out var e) && Enum.IsDefined(tipoBase, e!)) return e;

        throw new InvalidOperationException($"Valor '{valor}' inválido para o campo '{campo}'.");
    }
}

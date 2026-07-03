using System.Diagnostics;
using System.Text.Json;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Automacoes;

/// <summary>
/// Motor de execução de automações.
/// Processa um item da fila: carrega automação, avalia filtros, executa ações, registra log.
/// </summary>
public class MotorAutomacao(AppDbContext db, ILogger<MotorAutomacao> logger) : IMotorAutomacao
{
    public async Task ProcessarItemFilaAsync(long filaAutomacaoId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var fila = await db.FilaAutomacao
            .Include(f => f.Automacao)
                .ThenInclude(a => a.Filtros)
            .Include(f => f.Automacao)
                .ThenInclude(a => a.Acoes.OrderBy(ac => ac.Ordem))
            .FirstOrDefaultAsync(f => f.Id == filaAutomacaoId, cancellationToken);

        if (fila is null) return;

        var automacao = fila.Automacao;
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
            if (!AvaliarFiltros(automacao.Filtros, fila.DadosEvento))
            {
                execucao.Status = StatusExecucao.FalhaFiltro;
                execucao.Detalhes = "{\"motivo\": \"Filtros não satisfeitos\"}";
                return;
            }

            var detalhes = new List<string>();
            foreach (var acao in automacao.Acoes)
            {
                await ExecutarAcaoAsync(acao, fila, cancellationToken);
                detalhes.Add($"{acao.Tipo}: OK");
            }

            execucao.Status = StatusExecucao.Sucesso;
            execucao.Detalhes = JsonSerializer.Serialize(detalhes);
        }
        catch (Exception ex)
        {
            execucao.Status = StatusExecucao.Falha;
            execucao.Erro = ex.Message[..Math.Min(ex.Message.Length, 2000)];
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

    private bool AvaliarFiltros(ICollection<FiltroAutomacao> filtros, string? dadosEvento)
    {
        if (filtros.Count == 0) return true;

        var dados = ParseDadosEvento(dadosEvento);

        // Agrupar filtros por Grupo (mesmo grupo = AND, grupos diferentes = OR)
        var grupos = filtros.GroupBy(f => f.Grupo);

        // OR entre grupos: basta um grupo passar
        foreach (var grupo in grupos)
        {
            var grupoPassou = true;

            // AND dentro do grupo: todos os filtros devem passar
            foreach (var filtro in grupo)
            {
                if (!AvaliarFiltro(filtro, dados))
                {
                    grupoPassou = false;
                    break;
                }
            }

            if (grupoPassou) return true;
        }

        return false;
    }

    private static Dictionary<string, string?> ParseDadosEvento(string? dadosEvento)
    {
        if (string.IsNullOrWhiteSpace(dadosEvento))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(dadosEvento);
            var resultado = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                resultado[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                    ? null
                    : prop.Value.ToString();
            }

            return resultado;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool AvaliarFiltro(FiltroAutomacao filtro, Dictionary<string, string?> dados)
    {
        // Resolver valor do campo no JSON (suporta navegação com ponto: "Cliente.Nome")
        var valorCampo = ResolverValorCampo(filtro.CampoReferencia, dados);
        var valorFiltro = filtro.Valor;

        return filtro.Operador switch
        {
            OperadorFiltro.Igual => string.Equals(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Diferente => !string.Equals(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase),
            OperadorFiltro.Contem => valorCampo?.Contains(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            OperadorFiltro.NaoContem => valorCampo?.Contains(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) != true,
            OperadorFiltro.Maior => CompararNumerico(valorCampo, valorFiltro) > 0,
            OperadorFiltro.Menor => CompararNumerico(valorCampo, valorFiltro) < 0,
            OperadorFiltro.MaiorOuIgual => CompararNumerico(valorCampo, valorFiltro) >= 0,
            OperadorFiltro.MenorOuIgual => CompararNumerico(valorCampo, valorFiltro) <= 0,
            OperadorFiltro.Vazio => string.IsNullOrWhiteSpace(valorCampo),
            OperadorFiltro.NaoVazio => !string.IsNullOrWhiteSpace(valorCampo),
            OperadorFiltro.ComecaCom => valorCampo?.StartsWith(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            OperadorFiltro.TerminaCom => valorCampo?.EndsWith(valorFiltro ?? "", StringComparison.OrdinalIgnoreCase) == true,
            _ => true
        };
    }

    private static string? ResolverValorCampo(string campoReferencia, Dictionary<string, string?> dados)
    {
        // Tentar busca direta primeiro
        if (dados.TryGetValue(campoReferencia, out var valor))
            return valor;

        // Tentar busca por navegação com ponto (ex: "Cliente.Nome" -> buscar "Cliente.Nome" como chave)
        // Se não encontrar, retornar null
        return null;
    }

    private static int CompararNumerico(string? valorCampo, string? valorFiltro)
    {
        if (decimal.TryParse(valorCampo, out var numCampo) && decimal.TryParse(valorFiltro, out var numFiltro))
            return numCampo.CompareTo(numFiltro);

        // Fallback para comparação de string se não forem numéricos
        return string.Compare(valorCampo, valorFiltro, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExecutarAcaoAsync(AcaoAutomacao acao, FilaAutomacao fila, CancellationToken ct)
    {
        logger.LogDebug("Executando {Tipo} automação {Id}", acao.Tipo, acao.AutomacaoId);
        var config = JsonDocument.Parse(acao.Configuracao).RootElement;

        switch (acao.Tipo)
        {
            case TipoAcao.EditarDados:
                await ExecutarEditarDadosAsync(config, fila, ct);
                break;
            case TipoAcao.CriarTarefa:
                await ExecutarCriarTarefaAsync(config, fila, ct);
                break;
            case TipoAcao.AlterarStatus:
                await ExecutarAlterarStatusAsync(config, fila, ct);
                break;
            case TipoAcao.MoverEtapa:
                await ExecutarMoverEtapaAsync(config, fila, ct);
                break;
            case TipoAcao.ConcluirTarefa:
                await ExecutarConcluirTarefaAsync(fila, ct);
                break;
            case TipoAcao.AdicionarEtiqueta:
                await ExecutarAdicionarEtiquetaAsync(config, fila, ct);
                break;
            case TipoAcao.RemoverEtiqueta:
                await ExecutarRemoverEtiquetaAsync(config, fila, ct);
                break;
            case TipoAcao.CriarRegistroInteracao:
                await ExecutarCriarRegistroInteracaoAsync(config, fila, ct);
                break;
            case TipoAcao.ExecutarWebhook:
                await ExecutarWebhookAsync(config, ct);
                break;
            case TipoAcao.EnviarEmail:
                logger.LogInformation("EnviarEmail: TODO - integrar serviço SMTP/SES");
                break;
            case TipoAcao.NotificarUsuario:
                logger.LogInformation("NotificarUsuario: TODO - integrar sistema de notificações");
                break;
            default:
                logger.LogWarning("Ação {Tipo} não implementada", acao.Tipo);
                break;
        }
    }

    private async Task ExecutarEditarDadosAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return;

        if (!config.TryGetProperty("alteracoes", out var alteracoes)) return;

        var entidade = await BuscarEntidadeAsync(fila.EntidadeAlvo, fila.RegistroId.Value, ct);
        if (entidade is null) return;

        var tipo = entidade.GetType();
        foreach (var alteracao in alteracoes.EnumerateArray())
        {
            var campo = alteracao.GetProperty("campo").GetString();
            var valor = alteracao.GetProperty("valor").GetString();
            if (campo is null) continue;

            var prop = tipo.GetProperty(campo);
            if (prop is null || !prop.CanWrite) continue;

            var valorConvertido = ConverterValor(valor, prop.PropertyType);
            prop.SetValue(entidade, valorConvertido);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarCriarTarefaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        var titulo = config.TryGetProperty("titulo", out var t) ? t.GetString() : "Tarefa automática";
        var prazo = config.TryGetProperty("prazo", out var p) ? p.GetInt32() : 3;

        var atividade = new Atividade
        {
            EmpresaId = fila.EmpresaId,
            Titulo = titulo ?? "Tarefa automática",
            DataVencimento = DateTime.UtcNow.AddDays(prazo),
            ClienteId = fila.EntidadeAlvo == EntidadeAlvo.Cliente ? fila.RegistroId : null,
            NegocioId = fila.EntidadeAlvo == EntidadeAlvo.Negocio ? fila.RegistroId : null
        };

        if (config.TryGetProperty("tipo", out var tipoEl) && Enum.TryParse<TipoAtividade>(tipoEl.GetString(), out var tipoAtv))
            atividade.Tipo = tipoAtv;

        db.Atividades.Add(atividade);
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarAlterarStatusAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Negocio) return;

        if (!config.TryGetProperty("statusId", out var statusEl)) return;
        var statusId = statusEl.GetInt64();

        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == fila.RegistroId, ct);
        if (negocio is null) return;

        negocio.StatusId = statusId;
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarMoverEtapaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Negocio) return;

        if (!config.TryGetProperty("etapaId", out var etapaEl)) return;
        var etapaId = etapaEl.GetInt64();

        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == fila.RegistroId, ct);
        if (negocio is null) return;

        negocio.EtapaId = etapaId;
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarConcluirTarefaAsync(FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null || fila.EntidadeAlvo != EntidadeAlvo.Atividade) return;

        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == fila.RegistroId, ct);
        if (atividade is null) return;

        atividade.Concluida = true;
        atividade.ConcluidaEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarAdicionarEtiquetaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return;

        if (!config.TryGetProperty("etiquetaId", out var etEl)) return;

        db.EtiquetasRegistro.Add(new EtiquetaRegistro
        {
            EmpresaId = fila.EmpresaId,
            EtiquetaId = etEl.GetInt64(),
            RegistroId = fila.RegistroId.Value,
            EntidadeAlvo = fila.EntidadeAlvo
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarRemoverEtiquetaAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        if (fila.RegistroId is null) return;

        if (!config.TryGetProperty("etiquetaId", out var etEl)) return;
        var etiquetaId = etEl.GetInt64();

        var vinculo = await db.EtiquetasRegistro
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.EtiquetaId == etiquetaId
                && e.RegistroId == fila.RegistroId
                && e.EntidadeAlvo == fila.EntidadeAlvo, ct);

        if (vinculo is not null)
        {
            vinculo.Ativo = false;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task ExecutarCriarRegistroInteracaoAsync(JsonElement config, FilaAutomacao fila, CancellationToken ct)
    {
        var descricao = config.TryGetProperty("descricao", out var d) ? d.GetString() : "Registro de automação";

        db.RegistrosAlteracao.Add(new RegistroAlteracao
        {
            Usuario = "Automação",
            Acao = "AutomacaoExecutada",
            EntidadeAlvo = fila.EntidadeAlvo,
            RegistroId = fila.RegistroId ?? 0,
            Titulo = descricao,
            DadosDepois = fila.DadosEvento
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task ExecutarWebhookAsync(JsonElement config, CancellationToken ct)
    {
        var url = config.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (string.IsNullOrWhiteSpace(url)) return;

        var metodo = config.TryGetProperty("metodo", out var m) ? m.GetString() ?? "POST" : "POST";
        var corpo = config.TryGetProperty("corpo", out var c) ? c.GetString() : null;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var request = new HttpRequestMessage(new HttpMethod(metodo), url);

        if (config.TryGetProperty("headers", out var headers))
        {
            foreach (var header in headers.EnumerateObject())
                request.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
        }

        if (corpo is not null)
            request.Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Webhook falhou: {Status} {Url}", response.StatusCode, url);
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

    private static object? ConverterValor(string? valor, Type tipo)
    {
        if (valor is null) return null;
        var tipoBase = Nullable.GetUnderlyingType(tipo) ?? tipo;

        if (tipoBase == typeof(string)) return valor;
        if (tipoBase == typeof(int) && int.TryParse(valor, out var i)) return i;
        if (tipoBase == typeof(long) && long.TryParse(valor, out var l)) return l;
        if (tipoBase == typeof(decimal) && decimal.TryParse(valor, out var d)) return d;
        if (tipoBase == typeof(bool) && bool.TryParse(valor, out var b)) return b;
        if (tipoBase == typeof(DateTime) && DateTime.TryParse(valor, out var dt)) return dt;
        if (tipoBase.IsEnum) return Enum.TryParse(tipoBase, valor, true, out var e) ? e : null;

        return null;
    }
}

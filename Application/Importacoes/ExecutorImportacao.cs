using System.Text.Json;
using System.Text.Json.Nodes;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Importacoes;

/// <param name="MapeamentoBase">JSON com "mapeamento" e "opcoes"; o executor acrescenta "resumo" conforme avança.</param>
public sealed record ContextoExecucao(
    long EmpresaId, long? UsuarioId, string UsuarioNome, PerfilUsuario? Perfil,
    long ImportacaoId, string Entidade, OpcoesImportacao Opcoes, JsonObject MapeamentoBase);

/// <summary>
/// Grava as linhas válidas em segundo plano (a requisição de "executar" volta logo e a tela acompanha pelo histórico).
/// Os dados vivem só em memória; nenhum arquivo é guardado. Grava pelos serviços existentes (automações e auditoria disparam).
/// Lotes de 200 linhas em transação; se um lote falha, volta o lote e reprocessa linha a linha para isolar o erro,
/// sem desfazer os lotes anteriores.
/// </summary>
public sealed class ExecutorImportacao(
    IServiceScopeFactory escopos, IHostApplicationLifetime vida, ILogger<ExecutorImportacao> log)
{
    public const int TamanhoLote = 200;
    public const int MaxErrosGravados = 10_000;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private enum Resultado { Criada, Atualizada, Ignorada }

    private sealed class Totais
    {
        public int Criadas, Atualizadas, Ignoradas, Falhas;
        public List<ErroLinha> Erros = [];
        public int Processadas => Criadas + Atualizadas + Ignoradas + Falhas;
    }

    public void Iniciar(ContextoExecucao ctx, ResultadoPreparo preparo, int totalLinhas)
    {
        _ = Task.Run(() => ExecutarAsync(ctx, preparo, totalLinhas));
    }

    public static string SerializarErros(IEnumerable<ErroLinha> erros)
        => JsonSerializer.Serialize(erros.Take(MaxErrosGravados), Json);

    private async Task ExecutarAsync(ContextoExecucao ctx, ResultadoPreparo preparo, int totalLinhas)
    {
        var totais = new Totais();
        var parada = vida.ApplicationStopping;
        try
        {
            // Linhas inválidas já viram falha desde o início; só as válidas são gravadas.
            foreach (var l in preparo.Linhas.Where(l => !l.Valida)) totais.Erros.AddRange(l.Erros);
            totais.Falhas = preparo.Linhas.Count(l => !l.Valida);
            var validas = preparo.Linhas.Where(l => l.Valida).ToList();
            var indice = preparo.Indice ?? [];

            foreach (var lote in validas.Chunk(TamanhoLote))
            {
                if (parada.IsCancellationRequested) throw new OperationCanceledException();
                await ProcessarLoteAsync(ctx, preparo, lote, indice, totais, parada);
                await GravarProgressoAsync(ctx, totais, finalizar: false, status: StatusImportacao.Processando);
            }

            var sucesso = totais.Criadas + totais.Atualizadas;
            var status = totais.Falhas == 0 ? StatusImportacao.Concluida
                : sucesso + totais.Ignoradas == 0 ? StatusImportacao.Falhou
                : StatusImportacao.ConcluidaComErros;
            await GravarProgressoAsync(ctx, totais, finalizar: true, status: status);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Importação {Id} interrompida", ctx.ImportacaoId);
            var interrompida = ex is OperationCanceledException;
            totais.Erros.Add(new ErroLinha(0, "(importação)",
                interrompida ? "Importação interrompida (o servidor foi encerrado)." : "Falha inesperada durante a importação.", null));
            totais.Falhas += 1;
            try { await GravarProgressoAsync(ctx, totais, finalizar: true, status: StatusImportacao.Falhou); }
            catch (Exception ex2) { log.LogError(ex2, "Não foi possível registrar a falha da importação {Id}", ctx.ImportacaoId); }
        }
    }

    private async Task ProcessarLoteAsync(
        ContextoExecucao ctx, ResultadoPreparo preparo, LinhaPreparada[] lote, Dictionary<string, long> indice,
        Totais totais, CancellationToken ct)
    {
        using var escopo = escopos.CreateScope();
        var db = ConfigurarContexto(escopo.ServiceProvider, ctx);
        var adicionadas = new List<string>();
        var parcial = new Totais();
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            foreach (var linha in lote)
                Contar(parcial, await ProcessarLinhaAsync(escopo.ServiceProvider, db, ctx, preparo, linha, indice, adicionadas, ct));
            await tx.CommitAsync(ct);
            totais.Criadas += parcial.Criadas;
            totais.Atualizadas += parcial.Atualizadas;
            totais.Ignoradas += parcial.Ignoradas;
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Lote da importação {Id} falhou; reprocessando linha a linha", ctx.ImportacaoId);
        }

        // O lote voltou inteiro (transação desfeita). Desfaz também o índice e reprocessa linha a linha.
        foreach (var chave in adicionadas) indice.Remove(chave);
        db.ChangeTracker.Clear();

        foreach (var linha in lote)
        {
            ct.ThrowIfCancellationRequested();
            var adicionadasLinha = new List<string>();
            try
            {
                Contar(totais, await ProcessarLinhaAsync(escopo.ServiceProvider, db, ctx, preparo, linha, indice, adicionadasLinha, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                db.ChangeTracker.Clear();
                foreach (var chave in adicionadasLinha) indice.Remove(chave);
                totais.Falhas++;
                totais.Erros.Add(new ErroLinha(linha.Numero, "(linha)", MensagemSegura(ex), null));
            }
        }
    }

    private static void Contar(Totais t, Resultado r)
    {
        switch (r)
        {
            case Resultado.Criada: t.Criadas++; break;
            case Resultado.Atualizada: t.Atualizadas++; break;
            default: t.Ignoradas++; break;
        }
    }

    /// <summary>Mensagens de regra de negócio são mostradas; erros internos viram texto genérico.</summary>
    private static string MensagemSegura(Exception ex) => ex switch
    {
        RegraNegocioException r => r.Message,
        DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } } => "Já existe um registro com estes dados.",
        DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23503" } } => "Há referência a um registro que não existe.",
        DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "22001" } } => "Há texto maior que o permitido para o campo.",
        _ => "Não foi possível gravar esta linha."
    };

    private static AppDbContext ConfigurarContexto(IServiceProvider sp, ContextoExecucao ctx)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        db.EmpresaIdAtual = ctx.EmpresaId;
        db.UsuarioAtual = ctx.UsuarioNome;
        db.UsuarioNomeAtual = ctx.UsuarioNome;
        db.UsuarioIdAtual = ctx.UsuarioId;
        db.PerfilAtual = ctx.Perfil;
        return db;
    }

    // ------------------------------------------------------------------ uma linha

    private static Delta<T> MontarDelta<T>(params Dictionary<string, object>[] fontes) where T : class
    {
        var delta = new Delta<T>();
        foreach (var fonte in fontes)
            foreach (var (nome, valor) in fonte)
                if (!delta.TrySetPropertyValue(nome, valor))
                    throw new InvalidOperationException($"A propriedade {nome} não aceita o valor informado.");
        return delta;
    }

    private async Task<Resultado> ProcessarLinhaAsync(
        IServiceProvider sp, AppDbContext db, ContextoExecucao ctx, ResultadoPreparo preparo, LinhaPreparada linha,
        Dictionary<string, long> indice, List<string> adicionadas, CancellationToken ct)
    {
        long? existente = null;
        if (ctx.Opcoes.VerificaDuplicidade && linha.ChaveDuplicidade is not null && indice.TryGetValue(linha.ChaveDuplicidade, out var idExistente))
            existente = idExistente;

        if (existente is not null && ctx.Opcoes.Duplicidade == DuplicidadeModo.Ignorar)
            return Resultado.Ignorada;

        long registroId;
        var atualizando = existente is not null;
        if (atualizando)
        {
            registroId = existente!.Value;
            var ok = await AtualizarAsync(sp, ctx.Entidade, registroId, linha, ct);
            if (!ok) throw new RegraNegocioException("O registro a atualizar não foi encontrado.");
        }
        else
        {
            registroId = await CriarAsync(sp, ctx.Entidade, linha, ct);
            if (ctx.Opcoes.VerificaDuplicidade && linha.ChaveDuplicidade is not null)
            {
                indice[linha.ChaveDuplicidade] = registroId;
                adicionadas.Add(linha.ChaveDuplicidade);
            }
        }

        if (linha.Personalizados.Count > 0)
            await GravarPersonalizadosAsync(db, ctx.Entidade, registroId, linha.Personalizados, ct);

        return atualizando ? Resultado.Atualizada : Resultado.Criada;
    }

    private static async Task<long> CriarAsync(IServiceProvider sp, string entidade, LinhaPreparada l, CancellationToken ct)
    {
        switch (entidade)
        {
            case CatalogoEntidades.Cliente:
                return (await sp.GetRequiredService<IClienteServico>().CriarAsync(MontarDelta<Cliente>(l.Props, l.PropsCriacao), ct)).Id;
            case CatalogoEntidades.PessoaContato:
                return (await sp.GetRequiredService<IPessoaContatoServico>().CriarAsync(MontarDelta<PessoaContato>(l.Props, l.PropsCriacao), ct)).Id;
            case CatalogoEntidades.Produto:
                return (await sp.GetRequiredService<IProdutoServico>().CriarAsync(MontarDelta<Produto>(l.Props, l.PropsCriacao), ct)).Id;
            case CatalogoEntidades.Negocio:
                return (await sp.GetRequiredService<INegocioServico>().CriarAsync(MontarDelta<Negocio>(l.Props, l.PropsCriacao), ct)).Id;
            default:
                throw new InvalidOperationException("Entidade não importável.");
        }
    }

    private static async Task<bool> AtualizarAsync(IServiceProvider sp, string entidade, long id, LinhaPreparada l, CancellationToken ct)
    {
        switch (entidade)
        {
            case CatalogoEntidades.Cliente:
                return await sp.GetRequiredService<IClienteServico>().AtualizarAsync(id, MontarDelta<Cliente>(l.Props), ct) is not null;
            case CatalogoEntidades.PessoaContato:
                return await sp.GetRequiredService<IPessoaContatoServico>().AtualizarAsync(id, MontarDelta<PessoaContato>(l.Props), ct) is not null;
            case CatalogoEntidades.Produto:
                return await sp.GetRequiredService<IProdutoServico>().AtualizarAsync(id, MontarDelta<Produto>(l.Props), ct) is not null;
            case CatalogoEntidades.Negocio:
                return await sp.GetRequiredService<INegocioServico>().AtualizarAsync(id, MontarDelta<Negocio>(l.Props), ct) is not null;
            default:
                return false;
        }
    }

    /// <summary>
    /// Grava valores de campos personalizados (ValorCampo) direto no contexto: o serviço de campos só faz Add + SaveChanges,
    /// e aqui também é preciso reaproveitar um valor já existente (índice único campo+registro) mesmo que esteja inativo.
    /// </summary>
    private static async Task GravarPersonalizadosAsync(
        AppDbContext db, string entidade, long registroId, List<ValorPersonalizado> valores, CancellationToken ct)
    {
        var alvo = CatalogoImportacao.EntidadeAlvoDe(entidade);
        var campoIds = valores.Select(v => v.Campo.CampoId!.Value).ToList();
        var existentes = await db.ValoresCampo.IgnoreQueryFilters()
            .Where(v => v.EmpresaId == db.EmpresaIdAtual && v.EntidadeAlvo == alvo && v.RegistroId == registroId && campoIds.Contains(v.CampoId))
            .ToDictionaryAsync(v => v.CampoId, ct);

        foreach (var v in valores)
        {
            var campoId = v.Campo.CampoId!.Value;
            if (!existentes.TryGetValue(campoId, out var vc))
            {
                vc = new ValorCampo
                {
                    EmpresaId = db.EmpresaIdAtual, CampoId = campoId, CampoChave = v.Campo.CampoChaveInterna ?? string.Empty,
                    RegistroId = registroId, EntidadeAlvo = alvo
                };
                db.ValoresCampo.Add(vc);
            }
            vc.Ativo = true;
            vc.ValorTexto = v.Texto;
            vc.ValorTextoGrande = v.TextoGrande;
            vc.ValorInteiro = v.Inteiro;
            vc.ValorDecimal = v.Decimal;
            vc.ValorDataHora = v.DataHora;
            vc.ValorBooleano = v.Booleano;
            vc.OpcaoValorId = v.OpcaoId;
            vc.OpcaoValorNome = v.OpcaoNome;
        }
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ progresso

    private async Task GravarProgressoAsync(ContextoExecucao ctx, Totais t, bool finalizar, StatusImportacao status)
    {
        using var escopo = escopos.CreateScope();
        var db = ConfigurarContexto(escopo.ServiceProvider, ctx);
        var imp = await db.Importacoes.FirstOrDefaultAsync(i => i.Id == ctx.ImportacaoId);
        if (imp is null) return;

        imp.Status = status;
        imp.Sucesso = t.Criadas + t.Atualizadas;
        imp.Falhas = t.Falhas;
        imp.ErrosJson = SerializarErros(t.Erros);
        var mapa = (JsonObject)JsonNode.Parse(ctx.MapeamentoBase.ToJsonString())!;
        mapa["resumo"] = new JsonObject
        {
            ["criadas"] = t.Criadas,
            ["atualizadas"] = t.Atualizadas,
            ["ignoradas"] = t.Ignoradas,
            ["processadas"] = t.Processadas,
            ["errosTruncados"] = t.Erros.Count > MaxErrosGravados
        };
        imp.MapeamentoJson = mapa.ToJsonString();
        if (finalizar) imp.ConcluidaEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}

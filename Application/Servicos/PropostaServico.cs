using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public partial class PropostaServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IPropostaServico
{
    public IQueryable<Proposta> ObterTodos()
        => db.Propostas.AsNoTracking();

    public async Task<Proposta?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Propostas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Proposta> CriarAsync(Delta<Proposta> delta, CancellationToken ct = default)
    {
        var entidade = new Proposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        // A chave pública só é gerada por Compartilhar (aleatória); nunca aceita do cliente
        entidade.EmpresaId = db.EmpresaIdAtual;
        entidade.Chave = null;
        entidade.Compartilhada = false;
        entidade.DataCompartilhamento = null;
        // A resposta pública só é gravada pelo link público (aceitar/recusar)
        entidade.StatusResposta = StatusRespostaProposta.Aguardando;
        entidade.RespondidaEm = null;
        entidade.RespondidaIp = null;
        entidade.RespondidaPorNome = null;
        entidade.RespondidaPorEmail = null;
        entidade.MotivoRecusa = null;
        db.Propostas.Add(entidade);
        await db.SaveChangesAsync(ct);

        if (!entidade.IsTemplate)
            await publicador.PublicarEventoAsync(new EventoAutomacao
            {
                EmpresaId = entidade.EmpresaId,
                RegistroId = entidade.Id,
                EntidadeAlvo = "Proposta",
                Gatilho = "PropostaCriada",
                DisparadoPor = db.UsuarioAtual
            }, ct);

        return entidade;
    }

    public async Task<Proposta?> AtualizarAsync(long id, Delta<Proposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return null;
        var chaveOriginal = entidade.Chave;
        var empresaOriginal = entidade.EmpresaId;
        var (respStatus, respEm, respIp, respNome, respEmail, respMotivo) =
            (entidade.StatusResposta, entidade.RespondidaEm, entidade.RespondidaIp, entidade.RespondidaPorNome, entidade.RespondidaPorEmail, entidade.MotivoRecusa);
        delta.Patch(entidade);
        (entidade.StatusResposta, entidade.RespondidaEm, entidade.RespondidaIp, entidade.RespondidaPorNome, entidade.RespondidaPorEmail, entidade.MotivoRecusa) =
            (respStatus, respEm, respIp, respNome, respEmail, respMotivo);
        NormalizarDatasUtc(entidade);
        // PATCH pode revogar a chave (null) ou descompartilhar, mas não escolher uma chave própria nem trocar de empresa
        if (entidade.Chave is not null && entidade.Chave != chaveOriginal) entidade.Chave = chaveOriginal;
        if (entidade.Compartilhada && string.IsNullOrWhiteSpace(entidade.Chave)) entidade.Compartilhada = false;
        entidade.EmpresaId = empresaOriginal;
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AprovarAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Proposta {id} não encontrada.");
        proposta.StatusAprovacao = StatusAprovacaoProposta.Aprovada;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = proposta.EmpresaId,
            RegistroId = proposta.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = "PropostaAprovada",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public async Task RejeitarAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Proposta {id} não encontrada.");
        proposta.StatusAprovacao = StatusAprovacaoProposta.Rejeitada;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = proposta.EmpresaId,
            RegistroId = proposta.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    // ---------------- Compartilhamento e recálculo ----------------

    public async Task<CompartilhamentoPropostaDto?> CompartilharAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proposta is null) return null;

        var primeiraVez = !proposta.Compartilhada || proposta.DataCompartilhamento is null;
        if (!proposta.Compartilhada) proposta.AceitaExternamente = true;

        if (string.IsNullOrWhiteSpace(proposta.Chave))
            proposta.Chave = GerarChave();
        proposta.Compartilhada = true;
        proposta.DataCompartilhamento ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (primeiraVez)
            await publicador.PublicarEventoAsync(new EventoAutomacao
            {
                EmpresaId = proposta.EmpresaId,
                RegistroId = proposta.Id,
                EntidadeAlvo = "Proposta",
                Gatilho = "PropostaEnviada",
                DisparadoPor = db.UsuarioAtual
            }, ct);

        return new CompartilhamentoPropostaDto(proposta.Chave!, $"/p/{proposta.Chave}");
    }

    /// <summary>
    /// Fórmula: total do item = quantidade × preço unitário × (1 − desconto% / 100), arredondado a 2 casas;
    /// Valor da proposta = Σ totais dos itens ativos − Desconto da proposta (valor absoluto) + CustoFrete, mínimo 0.
    /// </summary>
    public async Task<Proposta?> RecalcularAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proposta is null) return null;

        var itens = await db.ItensProposta.Where(i => i.PropostaId == id).ToListAsync(ct);
        decimal subtotal = 0;
        foreach (var item in itens)
        {
            var qtd = (decimal)(item.Quantidade ?? 0);
            var preco = item.PrecoUnitario ?? 0m;
            var fator = 1m - (decimal)(item.Desconto ?? 0) / 100m;
            if (fator < 0m) fator = 0m;
            item.Total = Arredondar(qtd * preco * fator);
            subtotal += item.Total.Value;
        }

        var valor = subtotal - (proposta.Desconto ?? 0m) + (proposta.CustoFrete ?? 0m);
        proposta.Valor = Arredondar(valor < 0m ? 0m : valor);
        await db.SaveChangesAsync(ct);
        // Recarrega sem rastreamento: a entidade rastreada tem Produtos↔Proposta ligados (ciclo na serialização)
        return await ObterPorIdAsync(id, ct);
    }

    /// <summary>
    /// O OData entrega DateTime com Kind=Local (vindo de DateTimeOffset) e o Npgsql exige UTC em timestamptz:
    /// converte todas as propriedades DateTime da entidade para UTC antes de salvar.
    /// </summary>
    private static void NormalizarDatasUtc(object entidade)
    {
        foreach (var p in entidade.GetType().GetProperties())
        {
            if (!p.CanRead || !p.CanWrite) continue;
            if (p.PropertyType != typeof(DateTime) && p.PropertyType != typeof(DateTime?)) continue;
            if (p.GetValue(entidade) is not DateTime dt || dt.Kind == DateTimeKind.Utc) continue;
            // Unspecified = horário já convertido pelo OData para o fuso local do servidor (padrão do AspNetCore.OData)
            var local = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Local) : dt;
            p.SetValue(entidade, local.ToUniversalTime());
        }
    }

    private static decimal Arredondar(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static string GerarChave()
        => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // ---------------- Área pública (anônima) ----------------

    private const string PrefixoResposta = "[Resposta pública]";

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Za-z0-9_\-]{20,100}$")]
    private static partial System.Text.RegularExpressions.Regex RegexChave();

    /// <summary>
    /// Resolve a proposta pela chave, ignorando o filtro de tenant (não há usuário). Só vale se a proposta está
    /// ativa, compartilhada e não expirada. Depois de achada, o tenant do contexto passa a ser o da proposta,
    /// de modo que todas as consultas seguintes continuam isoladas pelo filtro global.
    /// </summary>
    private async Task<Proposta?> ResolverPorChaveAsync(string chave, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(chave) || !RegexChave().IsMatch(chave)) return null;

        var agora = DateTime.UtcNow;
        var proposta = await db.Propostas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Chave == chave
                && p.Ativo && p.Compartilhada && !p.IsTemplate
                && (p.DataExpiracao == null || p.DataExpiracao > agora), ct);
        if (proposta is null) return null;

        db.EmpresaIdAtual = proposta.EmpresaId;
        db.UsuarioAtual = "Cliente (link público)";
        return proposta;
    }

    /// <summary>"Aceita"/"Recusada" quando já houve resposta pública; null enquanto aguarda.</summary>
    private static string? StatusResposta(Proposta p)
        => p.StatusResposta == StatusRespostaProposta.Aguardando ? null : p.StatusResposta.ToString();
    public async Task<PropostaPublicaDto?> ObterPublicaAsync(string chave, CancellationToken ct = default)
    {
        var p = await ResolverPorChaveAsync(chave, ct);
        if (p is null) return null;

        p.UltimaAberturaExterna = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var itens = await db.ItensProposta.AsNoTracking().Include(i => i.Produto)
            .Where(i => i.PropostaId == p.Id).OrderBy(i => i.Ordem).ThenBy(i => i.Id).ToListAsync(ct);
        var parcelas = await db.ParcelasProposta.AsNoTracking()
            .Where(x => x.PropostaId == p.Id).OrderBy(x => x.Numero).ToListAsync(ct);
        var empresaNome = await db.Empresas.AsNoTracking().Where(e => e.Id == p.EmpresaId).Select(e => e.Nome).FirstOrDefaultAsync(ct);
        var clienteNome = p.ClienteId is null ? null
            : await db.Clientes.AsNoTracking().Where(c => c.Id == p.ClienteId).Select(c => c.Nome).FirstOrDefaultAsync(ct);
        var moeda = p.MoedaId is null ? null
            // Moedas são um catálogo global (EmpresaId = 0): o filtro de tenant as esconderia.
            : await db.Moedas.IgnoreQueryFilters().AsNoTracking().Where(m => m.Id == p.MoedaId).Select(m => m.Codigo).FirstOrDefaultAsync(ct);

        var resposta = StatusResposta(p);
        return new PropostaPublicaDto
        {
            Numero = p.Numero,
            Titulo = p.Titulo,
            EmpresaNome = empresaNome,
            ClienteNome = clienteNome,
            Valor = p.Valor,
            Moeda = moeda,
            ValidadeAte = p.DataExpiracao,
            Status = resposta ?? "Aguardando",
            Itens = itens.Select(i => new ItemPropostaPublicoDto
            {
                Descricao = i.ProdutoNome ?? i.Produto?.Nome,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                Desconto = i.Desconto,
                Total = i.Total ?? (i.Quantidade is null || i.PrecoUnitario is null ? null
                    : Arredondar((decimal)i.Quantidade.Value * i.PrecoUnitario.Value * (1m - (decimal)(i.Desconto ?? 0) / 100m)))
            }).ToList(),
            Parcelas = parcelas.Select(x => new ParcelaPropostaPublicaDto { Numero = x.Numero, Vencimento = x.DataVencimento, Valor = x.Valor }).ToList(),
            DocumentoHtml = MontarDocumento(p),
            PodeResponder = p.AceitaExternamente && resposta is null
        };
    }

    public Task<RespostaPublicaResultado> AceitarPublicaAsync(string chave, string? nome, string? email, string? ip, CancellationToken ct = default)
    {
        nome = Limpar(nome);
        email = Limpar(email);
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 200)
            return Task.FromResult(new RespostaPublicaResultado(ResultadoRespostaPublica.Invalida, null, "Informe seu nome (até 200 caracteres)."));
        if (email is { Length: > 0 } && (email.Length > 254 || !EmailValido(email)))
            return Task.FromResult(new RespostaPublicaResultado(ResultadoRespostaPublica.Invalida, null, "E-mail inválido."));

        return ResponderAsync(chave, aceitar: true, nome, email, null, ip, ct);
    }

    public Task<RespostaPublicaResultado> RecusarPublicaAsync(string chave, string? motivo, string? ip, CancellationToken ct = default)
    {
        motivo = Limpar(motivo);
        if (motivo is { Length: > 1000 })
            return Task.FromResult(new RespostaPublicaResultado(ResultadoRespostaPublica.Invalida, null, "O motivo excede 1000 caracteres."));
        return ResponderAsync(chave, aceitar: false, null, null, motivo, ip, ct);
    }

    private async Task<RespostaPublicaResultado> ResponderAsync(string chave, bool aceitar, string? nome, string? email, string? motivo, string? ip, CancellationToken ct)
    {
        var p = await ResolverPorChaveAsync(chave, ct);
        if (p is null) return new RespostaPublicaResultado(ResultadoRespostaPublica.NaoEncontrada);
        if (!p.AceitaExternamente) return new RespostaPublicaResultado(ResultadoRespostaPublica.NaoAceitaResposta, null, "Esta proposta não aceita resposta online.");
        if (StatusResposta(p) is not null) return new RespostaPublicaResultado(ResultadoRespostaPublica.JaRespondida, null, "Esta proposta já foi respondida.");

        var status = aceitar ? "Aceita" : "Recusada";
        p.StatusResposta = aceitar ? StatusRespostaProposta.Aceita : StatusRespostaProposta.Recusada;
        p.RespondidaEm = DateTime.UtcNow;
        p.RespondidaIp = Truncar(Limpar(ip), 64);
        p.RespondidaPorNome = aceitar ? nome : null;
        p.RespondidaPorEmail = aceitar ? email : null;
        p.MotivoRecusa = aceitar ? null : motivo;
        p.StatusAprovacao = aceitar ? StatusAprovacaoProposta.Aprovada : StatusAprovacaoProposta.Rejeitada;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = p.EmpresaId,
            RegistroId = p.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = aceitar ? "PropostaAprovada" : "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new RespostaPublicaResultado(ResultadoRespostaPublica.Ok, status);
    }

    private static string? Truncar(string? s, int max) => s is null || s.Length <= max ? s : s[..max];

    /// <summary>Remove caracteres de controle e quebras de linha.</summary>
    private static string? Limpar(string? s)
    {
        if (s is null) return null;
        var limpo = new string(s.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return limpo.Length == 0 ? null : limpo;
    }

    private static bool EmailValido(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba > 0 && arroba == email.LastIndexOf('@') && arroba < email.Length - 3
            && email.IndexOf('.', arroba) > arroba + 1 && !email.Contains(' ');
    }

    private static string? MontarDocumento(Proposta p)
    {
        var partes = new[] { p.CodigoFonteHeader, p.CodigoFonteBody ?? p.CodigoFontePreview, p.CodigoFonteFooter }
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (partes.Count == 0) return null;
        return SanitizarHtml(string.Concat(partes));
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*(script|iframe|object|embed|form|link|meta|base|applet)\b[^>]*>.*?<\s*/\s*\1\s*>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline)]
    private static partial System.Text.RegularExpressions.Regex RegexBlocosPerigosos();

    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*/?\s*(script|iframe|object|embed|form|link|meta|base|applet)\b[^>]*>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex RegexTagsPerigosas();

    [System.Text.RegularExpressions.GeneratedRegex(@"[\s/""']on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex RegexEventos();

    [System.Text.RegularExpressions.GeneratedRegex(@"(javascript|vbscript)\s*:", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex RegexProtocolos();

    /// <summary>Defesa em profundidade: o front deve exibir o HTML em iframe com sandbox.</summary>
    private static string SanitizarHtml(string html)
    {
        html = RegexBlocosPerigosos().Replace(html, "");
        html = RegexTagsPerigosas().Replace(html, "");
        html = RegexEventos().Replace(html, " ");
        html = RegexProtocolos().Replace(html, "");
        return html;
    }

    public IQueryable<ItemProposta> ObterProdutos()
        => db.ItensProposta.AsNoTracking();

    public async Task<ItemProposta> CriarProdutoAsync(Delta<ItemProposta> delta, CancellationToken ct = default)
    {
        var entidade = new ItemProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        db.ItensProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ItemProposta?> AtualizarProdutoAsync(long id, Delta<ItemProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.ItensProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverProdutoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ItensProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<SecaoProposta> ObterSecoes()
        => db.SecoesProposta.AsNoTracking();

    public async Task<SecaoProposta> CriarSecaoAsync(Delta<SecaoProposta> delta, CancellationToken ct = default)
    {
        var entidade = new SecaoProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        db.SecoesProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<SecaoProposta?> AtualizarSecaoAsync(long id, Delta<SecaoProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.SecoesProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.SecoesProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<ParcelaProposta> ObterParcelas()
        => db.ParcelasProposta.AsNoTracking();

    public async Task<ParcelaProposta> CriarParcelaAsync(Delta<ParcelaProposta> delta, CancellationToken ct = default)
    {
        var entidade = new ParcelaProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        db.ParcelasProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ParcelaProposta?> AtualizarParcelaAsync(long id, Delta<ParcelaProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.ParcelasProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        NormalizarDatasUtc(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverParcelaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ParcelasProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

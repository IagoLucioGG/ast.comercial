using System.Text.Json;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class RelatorioServico(AppDbContext db) : IRelatorioServico
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---------- Catálogo e dados ----------
    public CatalogoRelatorioDto ObterCatalogo() => new()
    {
        Entidades = RelatorioMotor.Todas.Select(e => e.Catalogo()).ToList(),
        Periodos = RelatorioMotor.Periodos.Select(p => new OpcaoRelDto { Valor = p.Chave, Rotulo = p.Rotulo }).ToList(),
        Operadores = RelatorioMotor.Operadores.Select(p => new OpcaoRelDto { Valor = p.Chave, Rotulo = p.Rotulo }).ToList(),
        Tipos =
        [
            new() { Valor = "Numero", Rotulo = "Número" }, new() { Valor = "Barras", Rotulo = "Barras" },
            new() { Valor = "Linhas", Rotulo = "Linhas" }, new() { Valor = "Pizza", Rotulo = "Pizza / rosca" },
            new() { Valor = "Tabela", Rotulo = "Tabela" }
        ]
    };

    public Task<List<OpcaoRelDto>> ObterOpcoesAsync(string referencia, string? busca, CancellationToken ct)
        => RelatorioReferencias.OpcoesAsync(db, referencia, busca, ct);

    public Task<RespostaDadosDto> ConsultarDadosAsync(ConsultaDadosDto consulta, CancellationToken ct)
        => RelatorioMotor.Obter(consulta.Entidade).ExecutarAsync(db, consulta, ct);

    // ---------- Painéis ----------
    private IQueryable<PainelRelatorio> Visiveis(UsuarioRelatorio u)
        => db.PaineisRelatorio.Where(p => u.Administrador || p.Compartilhado || (u.Id != null && p.CriadoPorUsuarioId == u.Id));

    private static bool PodeEditar(PainelRelatorio p, UsuarioRelatorio u)
        => u.Administrador || (u.Id != null && p.CriadoPorUsuarioId == u.Id);

    public async Task<List<PainelSaidaDto>> ListarPaineisAsync(UsuarioRelatorio usuario, CancellationToken ct)
    {
        if (!await db.PaineisRelatorio.AnyAsync(ct))
            await CriarPainelPadraoAsync(usuario, ct);

        var paineis = await Visiveis(usuario).AsNoTracking()
            .OrderBy(p => p.Ordem).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.Nome, p.Descricao, p.Compartilhado, p.Ordem, p.CriadoPorUsuarioId,
                Criador = p.CriadoPorUsuario != null ? p.CriadoPorUsuario.Nome : null,
                Qtd = p.Graficos.Count(g => g.Ativo)
            }).ToListAsync(ct);
        return paineis.Select(p => new PainelSaidaDto
        {
            Id = p.Id, Nome = p.Nome, Descricao = p.Descricao, Compartilhado = p.Compartilhado, Ordem = p.Ordem,
            CriadoPorUsuarioId = p.CriadoPorUsuarioId, CriadoPorNome = p.Criador, QuantidadeGraficos = p.Qtd,
            PodeEditar = usuario.Administrador || (usuario.Id != null && p.CriadoPorUsuarioId == usuario.Id)
        }).ToList();
    }

    public async Task<PainelSaidaDto?> ObterPainelAsync(long id, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var p = await Visiveis(usuario).AsNoTracking().Include(x => x.CriadoPorUsuario)
            .Include(x => x.Graficos.Where(g => g.Ativo).OrderBy(g => g.Ordem).ThenBy(g => g.Id))
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ParaDto(p, usuario, true);
    }

    public async Task<PainelSaidaDto> CriarPainelAsync(PainelEntradaDto e, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var nome = ValidarNome(e.Nome);
        var ordem = (await db.PaineisRelatorio.MaxAsync(p => (int?)p.Ordem, ct) ?? -1) + 1;
        var painel = new PainelRelatorio
        {
            EmpresaId = db.EmpresaIdAtual, Nome = nome, Descricao = LimparTexto(e.Descricao, 1000),
            Compartilhado = e.Compartilhado ?? false, CriadoPorUsuarioId = usuario.Id, Ordem = ordem
        };
        db.PaineisRelatorio.Add(painel);
        await db.SaveChangesAsync(ct);
        return ParaDto(painel, usuario, false);
    }

    public async Task<PainelSaidaDto?> AtualizarPainelAsync(long id, PainelEntradaDto e, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var painel = await Visiveis(usuario).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (painel is null) return null;
        ExigirEdicao(painel, usuario);
        if (e.Nome is not null) painel.Nome = ValidarNome(e.Nome);
        if (e.Descricao is not null) painel.Descricao = LimparTexto(e.Descricao, 1000);
        if (e.Compartilhado is not null) painel.Compartilhado = e.Compartilhado.Value;
        await db.SaveChangesAsync(ct);
        return ParaDto(painel, usuario, false);
    }

    public async Task<bool> ExcluirPainelAsync(long id, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var painel = await Visiveis(usuario).Include(p => p.Graficos).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (painel is null) return false;
        ExigirEdicao(painel, usuario);
        painel.Ativo = false;
        foreach (var g in painel.Graficos) g.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Gráficos ----------
    public async Task<GraficoSaidaDto?> CriarGraficoAsync(long painelId, GraficoEntradaDto e, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var painel = await Visiveis(usuario).FirstOrDefaultAsync(p => p.Id == painelId, ct);
        if (painel is null) return null;
        ExigirEdicao(painel, usuario);
        var g = new GraficoRelatorio { EmpresaId = db.EmpresaIdAtual, PainelRelatorioId = painel.Id };
        Aplicar(g, e);
        g.Ordem = (await db.GraficosRelatorio.Where(x => x.PainelRelatorioId == painelId).MaxAsync(x => (int?)x.Ordem, ct) ?? -1) + 1;
        db.GraficosRelatorio.Add(g);
        await db.SaveChangesAsync(ct);
        return ParaDto(g);
    }

    public async Task<GraficoSaidaDto?> AtualizarGraficoAsync(long id, GraficoEntradaDto e, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var g = await db.GraficosRelatorio.Include(x => x.PainelRelatorio).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null || !(usuario.Administrador || g.PainelRelatorio.Compartilhado || g.PainelRelatorio.CriadoPorUsuarioId == usuario.Id)) return null;
        ExigirEdicao(g.PainelRelatorio, usuario);
        Aplicar(g, e);
        await db.SaveChangesAsync(ct);
        return ParaDto(g);
    }

    public async Task<bool> ExcluirGraficoAsync(long id, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var g = await db.GraficosRelatorio.Include(x => x.PainelRelatorio).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null || !(usuario.Administrador || g.PainelRelatorio.Compartilhado || g.PainelRelatorio.CriadoPorUsuarioId == usuario.Id)) return false;
        ExigirEdicao(g.PainelRelatorio, usuario);
        g.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReordenarGraficosAsync(long painelId, List<long> ids, UsuarioRelatorio usuario, CancellationToken ct)
    {
        var painel = await Visiveis(usuario).Include(p => p.Graficos).FirstOrDefaultAsync(p => p.Id == painelId, ct);
        if (painel is null) return false;
        ExigirEdicao(painel, usuario);
        var ativos = painel.Graficos.Where(g => g.Ativo).ToDictionary(g => g.Id);
        if (ids.Distinct().Count() != ids.Count || ids.Any(i => !ativos.ContainsKey(i)))
            throw new RegraNegocioException("A lista de gráficos para reordenar é inválida.");
        var ordem = 0;
        foreach (var id in ids) ativos[id].Ordem = ordem++;
        foreach (var g in ativos.Values.Where(g => !ids.Contains(g.Id))) g.Ordem = ordem++;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Auxiliares ----------
    private static void ExigirEdicao(PainelRelatorio p, UsuarioRelatorio u)
    {
        if (!PodeEditar(p, u))
            throw new AcessoRelatorioNegadoException("Somente o criador do painel ou um administrador pode alterá-lo.");
    }

    private static string ValidarNome(string? nome)
    {
        var n = nome?.Trim();
        if (string.IsNullOrEmpty(n)) throw new RegraNegocioException("Informe o nome do painel.");
        if (n.Length > 200) throw new RegraNegocioException("O nome do painel pode ter no máximo 200 caracteres.");
        return n;
    }

    private static string? LimparTexto(string? t, int max)
    {
        var v = t?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        return v.Length > max ? v[..max] : v;
    }

    private sealed class ConfigFiltro
    {
        public List<FiltroRelatorioDto> Filtros { get; set; } = [];
        public int? Limite { get; set; }
    }

    private static void Aplicar(GraficoRelatorio g, GraficoEntradaDto e)
    {
        var titulo = e.Titulo?.Trim();
        if (string.IsNullOrEmpty(titulo)) throw new RegraNegocioException("Informe o título do gráfico.");
        if (titulo.Length > 200) throw new RegraNegocioException("O título do gráfico pode ter no máximo 200 caracteres.");
        if (!Enum.TryParse<TipoGrafico>(e.Tipo, true, out var tipo) || !Enum.IsDefined(tipo))
            throw new RegraNegocioException($"Tipo de gráfico '{e.Tipo}' inválido.");
        if (!Enum.TryParse<AgregacaoGrafico>(e.Agregacao, true, out var agregacao) || !Enum.IsDefined(agregacao))
            throw new RegraNegocioException($"Agregação '{e.Agregacao}' inválida.");
        if (e.Largura is < 1 or > 4) throw new RegraNegocioException("A largura deve estar entre 1 e 4 colunas.");
        e.Agregacao = agregacao.ToString();
        // Valida a definição inteira contra a lista branca.
        RelatorioMotor.Obter(e.Entidade).Validar(e);

        g.Titulo = titulo;
        g.Tipo = tipo;
        g.Entidade = e.Entidade;
        g.Agregacao = agregacao;
        g.CampoValor = string.IsNullOrWhiteSpace(e.CampoValor) ? null : e.CampoValor;
        g.CampoAgrupamento = string.IsNullOrWhiteSpace(e.CampoAgrupamento) ? null : e.CampoAgrupamento;
        g.PeriodoCampo = string.IsNullOrWhiteSpace(e.PeriodoCampo) ? null : e.PeriodoCampo;
        g.PeriodoRelativo = string.IsNullOrWhiteSpace(e.PeriodoRelativo) || e.PeriodoRelativo == "todos" ? null : e.PeriodoRelativo;
        g.Largura = e.Largura;
        g.FiltroJson = e.Filtros.Count == 0 && e.Limite is null
            ? null
            : JsonSerializer.Serialize(new ConfigFiltro { Filtros = e.Filtros, Limite = e.Limite }, Json);
    }

    private static GraficoSaidaDto ParaDto(GraficoRelatorio g)
    {
        ConfigFiltro cfg = new();
        if (!string.IsNullOrWhiteSpace(g.FiltroJson))
        {
            try { cfg = JsonSerializer.Deserialize<ConfigFiltro>(g.FiltroJson, Json) ?? new(); }
            catch (JsonException) { cfg = new(); }
        }
        return new GraficoSaidaDto
        {
            Id = g.Id, PainelId = g.PainelRelatorioId, Titulo = g.Titulo, Tipo = g.Tipo.ToString(), Entidade = g.Entidade,
            Agregacao = g.Agregacao.ToString(), CampoValor = g.CampoValor, CampoAgrupamento = g.CampoAgrupamento,
            Filtros = cfg.Filtros, Limite = cfg.Limite, PeriodoCampo = g.PeriodoCampo, PeriodoRelativo = g.PeriodoRelativo,
            Largura = g.Largura, Ordem = g.Ordem
        };
    }

    private static PainelSaidaDto ParaDto(PainelRelatorio p, UsuarioRelatorio u, bool comGraficos) => new()
    {
        Id = p.Id, Nome = p.Nome, Descricao = p.Descricao, Compartilhado = p.Compartilhado, Ordem = p.Ordem,
        CriadoPorUsuarioId = p.CriadoPorUsuarioId, CriadoPorNome = p.CriadoPorUsuario?.Nome,
        PodeEditar = PodeEditar(p, u),
        QuantidadeGraficos = comGraficos ? p.Graficos.Count(g => g.Ativo) : 0,
        Graficos = comGraficos ? p.Graficos.Where(g => g.Ativo).OrderBy(g => g.Ordem).ThenBy(g => g.Id).Select(ParaDto).ToList() : null
    };

    // ---------- Painel padrão ----------
    private async Task CriarPainelPadraoAsync(UsuarioRelatorio usuario, CancellationToken ct)
    {
        var painel = new PainelRelatorio
        {
            EmpresaId = db.EmpresaIdAtual, Nome = "Visão comercial",
            Descricao = "Indicadores principais de negócios, atividades e clientes.",
            Compartilhado = true, CriadoPorUsuarioId = usuario.Id, Ordem = 0
        };
        FiltroRelatorioDto F(string campo, string valor, string op = "eq")
            => new() { Campo = campo, Operador = op, Valor = JsonSerializer.SerializeToElement(valor) };

        var graficos = new List<GraficoEntradaDto>
        {
            new() { Titulo = "Negócios em aberto por etapa", Tipo = "Barras", Entidade = "Negocio", Agregacao = "Contagem",
                    CampoAgrupamento = "Etapa", Filtros = [F("Situacao", "Aberto")], Largura = 2 },
            new() { Titulo = "Valor ganho por mês", Tipo = "Linhas", Entidade = "Negocio", Agregacao = "Soma", CampoValor = "Valor",
                    CampoAgrupamento = "FechadoEm", Filtros = [F("Situacao", "Ganho")], PeriodoCampo = "FechadoEm", PeriodoRelativo = "ano", Largura = 2 },
            new() { Titulo = "Conversão: negócios por situação", Tipo = "Pizza", Entidade = "Negocio", Agregacao = "Contagem",
                    CampoAgrupamento = "Situacao", Largura = 1 },
            new() { Titulo = "Valor em aberto", Tipo = "Numero", Entidade = "Negocio", Agregacao = "Soma", CampoValor = "Valor",
                    Filtros = [F("Situacao", "Aberto")], Largura = 1 },
            new() { Titulo = "Atividades por tipo (90 dias)", Tipo = "Pizza", Entidade = "Atividade", Agregacao = "Contagem",
                    CampoAgrupamento = "Tipo", PeriodoCampo = "CriadoEm", PeriodoRelativo = "90d", Largura = 2 },
            new() { Titulo = "Top 5 clientes por valor ganho", Tipo = "Tabela", Entidade = "Negocio", Agregacao = "Soma", CampoValor = "Valor",
                    CampoAgrupamento = "Cliente", Filtros = [F("Situacao", "Ganho")], Limite = 5, Largura = 2 },
        };
        var ordem = 0;
        foreach (var e in graficos)
        {
            var g = new GraficoRelatorio { EmpresaId = db.EmpresaIdAtual, Ordem = ordem++ };
            Aplicar(g, e);
            painel.Graficos.Add(g);
        }
        db.PaineisRelatorio.Add(painel);
        await db.SaveChangesAsync(ct);
    }
}

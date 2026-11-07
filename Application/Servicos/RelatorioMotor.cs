using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using AST.Comercial.Application.Dtos;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

/// <summary>
/// Motor de agregação dos relatórios. SEGURANÇA: não há expressão livre. Cada entidade expõe uma lista branca
/// de campos (valor, agrupamento, filtro, período) já mapeados para expressões tipadas; o cliente só escolhe
/// chaves dessa lista, operadores fixos e valores que são convertidos para o tipo do campo.
/// </summary>
public static class RelatorioMotor
{
    public static readonly string[] Agregacoes = ["Contagem", "Soma", "Media"];

    public static readonly (string Chave, string Rotulo)[] Periodos =
    [
        ("hoje", "Hoje"), ("7d", "Últimos 7 dias"), ("30d", "Últimos 30 dias"), ("90d", "Últimos 90 dias"),
        ("mes", "Este mês"), ("mes_anterior", "Mês anterior"), ("ano", "Este ano"), ("ano_anterior", "Ano anterior"),
        ("proximos_30d", "Próximos 30 dias")
    ];

    public static readonly (string Chave, string Rotulo)[] Operadores =
    [
        ("eq", "é igual a"), ("ne", "é diferente de"), ("gt", "maior que"), ("gte", "maior ou igual a"),
        ("lt", "menor que"), ("lte", "menor ou igual a"), ("contem", "contém"),
        ("nulo", "está vazio"), ("naonulo", "está preenchido")
    ];

    private static readonly Dictionary<string, string[]> OperadoresPorTipo = new()
    {
        ["id"] = ["eq", "ne", "nulo", "naonulo"],
        ["numero"] = ["eq", "ne", "gt", "gte", "lt", "lte"],
        ["data"] = ["gt", "gte", "lt", "lte", "nulo", "naonulo"],
        ["texto"] = ["eq", "ne", "contem", "nulo", "naonulo"],
        ["bool"] = ["eq"],
        ["enum"] = ["eq", "ne"],
    };

    private static readonly string[] Meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    private static readonly Expression<Func<Negocio, string?>> SituacaoNegocioExpr =
        e => e.Status != null && e.Status.TipoBase == TipoStatusNegocio.Ganho ? "Ganho"
           : e.Status != null && e.Status.TipoBase == TipoStatusNegocio.Perdido ? "Perdido" : "Aberto";

    // ---------- Registro das entidades ----------
    private static readonly Dictionary<string, IEntidadeRel> Entidades = Construir();

    public static IEnumerable<IEntidadeRel> Todas => Entidades.Values;

    public static IEntidadeRel Obter(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave) || !Entidades.TryGetValue(chave, out var e))
            throw new RegraNegocioException($"Entidade '{chave}' não está disponível para relatórios.");
        return e;
    }


    /// <summary>Converte um período relativo em intervalo UTC [início, fim).</summary>
    public static (DateTime Inicio, DateTime Fim)? ResolverPeriodo(string? relativo, DateTime? agora = null)
    {
        if (string.IsNullOrWhiteSpace(relativo)) return null;
        var hoje = (agora ?? DateTime.UtcNow).Date;
        hoje = DateTime.SpecifyKind(hoje, DateTimeKind.Utc);
        var amanha = hoje.AddDays(1);
        var iniMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var iniAno = new DateTime(hoje.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return relativo switch
        {
            "hoje" => (hoje, amanha),
            "7d" => (hoje.AddDays(-6), amanha),
            "30d" => (hoje.AddDays(-29), amanha),
            "90d" => (hoje.AddDays(-89), amanha),
            "mes" => (iniMes, iniMes.AddMonths(1)),
            "mes_anterior" => (iniMes.AddMonths(-1), iniMes),
            "ano" => (iniAno, iniAno.AddYears(1)),
            "ano_anterior" => (iniAno.AddYears(-1), iniAno),
            "proximos_30d" => (hoje, hoje.AddDays(30)),
            "todos" => null,
            _ => throw new RegraNegocioException($"Período '{relativo}' inválido.")
        };
    }

    public static string RotuloMes(string chave)
    {
        if (chave.Length == 6 && int.TryParse(chave[..4], out var a) && int.TryParse(chave[4..], out var m) && m is >= 1 and <= 12)
            return $"{Meses[m - 1]}/{a}";
        return chave;
    }

    // ---------- Definições ----------
    private static Dictionary<string, IEntidadeRel> Construir()
    {
        var dic = new Dictionary<string, IEntidadeRel>(StringComparer.Ordinal);
        var situacaoNegocio = new[] { O("Aberto", "Em aberto"), O("Ganho", "Ganho"), O("Perdido", "Perdido") };

        // ----- Negócios -----
        var negocio = new EntidadeRel<Negocio>("Negocio", "Negócios", db => db.Negocios.AsNoTracking(), e => e.MoedaId, "CriadoEm");
        negocio.Valores.Add(new("Valor", "Valor", true, e => e.Valor));
        negocio.Dims.Add(new("Etapa", "Etapa do funil", "id", e => e.EtapaId.ToString(), "EtapaFunil"));
        negocio.Dims.Add(new("Funil", "Funil", "id", e => e.FunilId.ToString(), "Funil"));
        negocio.Dims.Add(new("Status", "Status", "id", e => e.StatusId == null ? null : e.StatusId.Value.ToString(), "StatusNegocio"));
        negocio.Dims.Add(new("Situacao", "Situação (aberto/ganho/perdido)", "texto", SituacaoNegocioExpr, null) { Rotulos = Dic(situacaoNegocio) });
        negocio.Dims.Add(new("Responsavel", "Responsável", "id", e => e.ResponsavelId == null ? null : e.ResponsavelId.Value.ToString(), "Usuario"));
        negocio.Dims.Add(new("Moeda", "Moeda", "id", e => e.MoedaId == null ? null : e.MoedaId.Value.ToString(), "Moeda"));
        negocio.Dims.Add(new("Cliente", "Cliente", "id", e => e.ClienteId.ToString(), "Cliente"));
        negocio.Dims.Add(new("MotivoPerda", "Motivo de perda", "id", e => e.MotivoPerdaId == null ? null : e.MotivoPerdaId.Value.ToString(), "MotivoPerda"));
        negocio.Dims.Add(new("CriadoEm", "Mês de criação", "mes", e => (e.CriadoEm.Year * 100 + e.CriadoEm.Month).ToString(), null));
        negocio.Dims.Add(new("FechadoEm", "Mês de fechamento", "mes", e => e.FechadoEm == null ? null : (e.FechadoEm.Value.Year * 100 + e.FechadoEm.Value.Month).ToString(), null));
        negocio.Dims.Add(new("PrevisaoFechamento", "Mês da previsão de fechamento", "mes", e => e.PrevisaoFechamento == null ? null : (e.PrevisaoFechamento.Value.Year * 100 + e.PrevisaoFechamento.Value.Month).ToString(), null));
        negocio.Filtros.Add(Cmp<Negocio, string?>("Situacao", "Situação", "enum", SituacaoNegocioExpr, situacaoNegocio));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Etapa", "Etapa do funil", "id", e => (long?)e.EtapaId, ref_: "EtapaFunil"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Funil", "Funil", "id", e => (long?)e.FunilId, ref_: "Funil"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Status", "Status", "id", e => e.StatusId, ref_: "StatusNegocio"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Responsavel", "Responsável", "id", e => e.ResponsavelId, ref_: "Usuario"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Moeda", "Moeda", "id", e => e.MoedaId, ref_: "Moeda"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("Cliente", "Cliente", "id", e => (long?)e.ClienteId, ref_: "Cliente"));
        negocio.Filtros.Add(Cmp<Negocio, long?>("MotivoPerda", "Motivo de perda", "id", e => e.MotivoPerdaId, ref_: "MotivoPerda"));
        negocio.Filtros.Add(Cmp<Negocio, string?>("Titulo", "Título", "texto", e => e.Titulo));
        negocio.Filtros.Add(Cmp<Negocio, decimal?>("Valor", "Valor", "numero", e => e.Valor));
        negocio.Filtros.Add(Cmp<Negocio, DateTime?>("CriadoEm", "Data de criação", "data", e => e.CriadoEm));
        negocio.Filtros.Add(Cmp<Negocio, DateTime?>("FechadoEm", "Data de fechamento", "data", e => e.FechadoEm));
        negocio.Filtros.Add(Cmp<Negocio, DateTime?>("PrevisaoFechamento", "Previsão de fechamento", "data", e => e.PrevisaoFechamento));
        negocio.Periodos.AddRange(["CriadoEm", "FechadoEm", "PrevisaoFechamento"]);
        dic[negocio.Chave] = negocio;

        // ----- Atividades -----
        var tipoAtiv = new[] { O("Tarefa", "Tarefa"), O("Ligacao", "Ligação"), O("Email", "E-mail"), O("Reuniao", "Reunião"), O("Nota", "Nota") };
        var atividade = new EntidadeRel<Atividade>("Atividade", "Atividades", db => db.Atividades.AsNoTracking(), null, "CriadoEm");
        atividade.Dims.Add(new("Tipo", "Tipo", "texto", EnumTexto<Atividade, TipoAtividade>(e => e.Tipo), null) { Rotulos = Dic(tipoAtiv) });
        atividade.Dims.Add(new("Situacao", "Situação (concluída/pendente)", "texto", e => e.Concluida ? "Concluida" : "Pendente", null) { Rotulos = Dic([O("Concluida", "Concluída"), O("Pendente", "Pendente")]) });
        atividade.Dims.Add(new("Responsavel", "Responsável", "id", e => e.ResponsavelId == null ? null : e.ResponsavelId.Value.ToString(), "Usuario"));
        atividade.Dims.Add(new("Cliente", "Cliente", "id", e => e.ClienteId == null ? null : e.ClienteId.Value.ToString(), "Cliente"));
        atividade.Dims.Add(new("DataVencimento", "Mês de vencimento", "mes", e => e.DataVencimento == null ? null : (e.DataVencimento.Value.Year * 100 + e.DataVencimento.Value.Month).ToString(), null));
        atividade.Dims.Add(new("ConcluidaEm", "Mês de conclusão", "mes", e => e.ConcluidaEm == null ? null : (e.ConcluidaEm.Value.Year * 100 + e.ConcluidaEm.Value.Month).ToString(), null));
        atividade.Dims.Add(new("CriadoEm", "Mês de criação", "mes", e => (e.CriadoEm.Year * 100 + e.CriadoEm.Month).ToString(), null));
        atividade.Filtros.Add(Cmp<Atividade, TipoAtividade>("Tipo", "Tipo", "enum", e => e.Tipo, tipoAtiv));
        atividade.Filtros.Add(Cmp<Atividade, bool>("Concluida", "Concluída", "bool", e => e.Concluida));
        atividade.Filtros.Add(Cmp<Atividade, long?>("Responsavel", "Responsável", "id", e => e.ResponsavelId, ref_: "Usuario"));
        atividade.Filtros.Add(Cmp<Atividade, long?>("Cliente", "Cliente", "id", e => e.ClienteId, ref_: "Cliente"));
        atividade.Filtros.Add(Cmp<Atividade, string?>("Titulo", "Título", "texto", e => e.Titulo));
        atividade.Filtros.Add(Cmp<Atividade, DateTime?>("DataVencimento", "Data de vencimento", "data", e => e.DataVencimento));
        atividade.Filtros.Add(Cmp<Atividade, DateTime?>("ConcluidaEm", "Data de conclusão", "data", e => e.ConcluidaEm));
        atividade.Filtros.Add(Cmp<Atividade, DateTime?>("CriadoEm", "Data de criação", "data", e => e.CriadoEm));
        atividade.Periodos.AddRange(["CriadoEm", "DataVencimento", "ConcluidaEm"]);
        dic[atividade.Chave] = atividade;

        // ----- Propostas -----
        var statusResp = new[] { O("Aguardando", "Aguardando resposta"), O("Aceita", "Aceita"), O("Recusada", "Recusada") };
        var statusAprov = new[] { O("Pendente", "Pendente"), O("Aprovada", "Aprovada"), O("Rejeitada", "Rejeitada"), O("EmRevisao", "Em revisão") };
        var proposta = new EntidadeRel<Proposta>("Proposta", "Propostas", db => db.Propostas.AsNoTracking().Where(p => !p.IsTemplate), e => e.MoedaId, "Data");
        proposta.Valores.Add(new("Valor", "Valor", true, e => e.Valor));
        proposta.Valores.Add(new("Desconto", "Desconto", true, e => e.Desconto));
        proposta.Dims.Add(new("StatusResposta", "Resposta do cliente", "texto", EnumTexto<Proposta, StatusRespostaProposta>(e => e.StatusResposta), null) { Rotulos = Dic(statusResp) });
        proposta.Dims.Add(new("StatusAprovacao", "Aprovação interna", "texto", EnumTexto<Proposta, StatusAprovacaoProposta?>(e => e.StatusAprovacao), null) { Rotulos = Dic(statusAprov) });
        proposta.Dims.Add(new("Cliente", "Cliente", "id", e => e.ClienteId == null ? null : e.ClienteId.Value.ToString(), "Cliente"));
        proposta.Dims.Add(new("Responsavel", "Responsável", "id", e => e.ResponsavelId == null ? null : e.ResponsavelId.Value.ToString(), "Usuario"));
        proposta.Dims.Add(new("Moeda", "Moeda", "id", e => e.MoedaId == null ? null : e.MoedaId.Value.ToString(), "Moeda"));
        proposta.Dims.Add(new("Data", "Mês da proposta", "mes", e => (e.Data.Year * 100 + e.Data.Month).ToString(), null));
        proposta.Dims.Add(new("RespondidaEm", "Mês da resposta", "mes", e => e.RespondidaEm == null ? null : (e.RespondidaEm.Value.Year * 100 + e.RespondidaEm.Value.Month).ToString(), null));
        proposta.Filtros.Add(Cmp<Proposta, StatusRespostaProposta>("StatusResposta", "Resposta do cliente", "enum", e => e.StatusResposta, statusResp));
        proposta.Filtros.Add(Cmp<Proposta, StatusAprovacaoProposta?>("StatusAprovacao", "Aprovação interna", "enum", e => e.StatusAprovacao, statusAprov));
        proposta.Filtros.Add(Cmp<Proposta, long?>("Cliente", "Cliente", "id", e => e.ClienteId, ref_: "Cliente"));
        proposta.Filtros.Add(Cmp<Proposta, long?>("Responsavel", "Responsável", "id", e => e.ResponsavelId, ref_: "Usuario"));
        proposta.Filtros.Add(Cmp<Proposta, long?>("Moeda", "Moeda", "id", e => e.MoedaId, ref_: "Moeda"));
        proposta.Filtros.Add(Cmp<Proposta, bool>("Compartilhada", "Compartilhada", "bool", e => e.Compartilhada));
        proposta.Filtros.Add(Cmp<Proposta, decimal?>("Valor", "Valor", "numero", e => e.Valor));
        proposta.Filtros.Add(Cmp<Proposta, DateTime?>("Data", "Data da proposta", "data", e => e.Data));
        proposta.Filtros.Add(Cmp<Proposta, DateTime?>("RespondidaEm", "Data da resposta", "data", e => e.RespondidaEm));
        proposta.Periodos.AddRange(["Data", "RespondidaEm", "CriadoEm"]);
        proposta.Filtros.Add(Cmp<Proposta, DateTime?>("CriadoEm", "Data de criação", "data", e => e.CriadoEm));
        dic[proposta.Chave] = proposta;

        // ----- Clientes -----
        var cliente = new EntidadeRel<Cliente>("Cliente", "Clientes", db => db.Clientes.AsNoTracking(), null, "CriadoEm");
        cliente.Dims.Add(new("Status", "Status", "id", e => e.StatusClienteId == null ? null : e.StatusClienteId.Value.ToString(), "StatusCliente"));
        cliente.Dims.Add(new("Origem", "Origem", "id", e => e.OrigemId == null ? null : e.OrigemId.Value.ToString(), "Origem"));
        cliente.Dims.Add(new("Ramo", "Ramo de atividade", "id", e => e.RamoAtividadeId == null ? null : e.RamoAtividadeId.Value.ToString(), "Ramo"));
        cliente.Dims.Add(new("Porte", "Porte", "id", e => e.PorteEmpresaId == null ? null : e.PorteEmpresaId.Value.ToString(), "Porte"));
        cliente.Dims.Add(new("Estado", "Estado", "texto", e => e.Estado, null));
        cliente.Dims.Add(new("Cidade", "Cidade", "texto", e => e.Cidade, null));
        cliente.Dims.Add(new("CriadoEm", "Mês de criação", "mes", e => (e.CriadoEm.Year * 100 + e.CriadoEm.Month).ToString(), null));
        cliente.Filtros.Add(Cmp<Cliente, long?>("Status", "Status", "id", e => e.StatusClienteId, ref_: "StatusCliente"));
        cliente.Filtros.Add(Cmp<Cliente, long?>("Origem", "Origem", "id", e => e.OrigemId, ref_: "Origem"));
        cliente.Filtros.Add(Cmp<Cliente, long?>("Ramo", "Ramo de atividade", "id", e => e.RamoAtividadeId, ref_: "Ramo"));
        cliente.Filtros.Add(Cmp<Cliente, long?>("Porte", "Porte", "id", e => e.PorteEmpresaId, ref_: "Porte"));
        cliente.Filtros.Add(Cmp<Cliente, string?>("Estado", "Estado", "texto", e => e.Estado));
        cliente.Filtros.Add(Cmp<Cliente, string?>("Cidade", "Cidade", "texto", e => e.Cidade));
        cliente.Filtros.Add(Cmp<Cliente, string?>("Nome", "Nome", "texto", e => e.Nome));
        cliente.Filtros.Add(Cmp<Cliente, DateTime?>("CriadoEm", "Data de criação", "data", e => e.CriadoEm));
        cliente.Periodos.Add("CriadoEm");
        dic[cliente.Chave] = cliente;

        // ----- Itens do negócio -----
        var item = new EntidadeRel<ItemNegocio>("ItemNegocio", "Itens de negócio", db => db.ItensNegocio.AsNoTracking(), e => e.Negocio.MoedaId, "CriadoEm");
        item.Valores.Add(new("Total", "Total do item", true, e => (decimal?)e.Total));
        item.Valores.Add(new("PrecoUnitario", "Preço unitário", true, e => (decimal?)e.PrecoUnitario));
        item.Valores.Add(new("Quantidade", "Quantidade", false, e => (decimal?)e.Quantidade));
        item.Dims.Add(new("Produto", "Produto", "id", e => e.ProdutoId == null ? null : e.ProdutoId.Value.ToString(), "Produto"));
        item.Dims.Add(new("Responsavel", "Responsável pelo negócio", "id", e => e.Negocio.ResponsavelId == null ? null : e.Negocio.ResponsavelId.Value.ToString(), "Usuario"));
        item.Dims.Add(new("Cliente", "Cliente do negócio", "id", e => e.Negocio.ClienteId.ToString(), "Cliente"));
        item.Dims.Add(new("Situacao", "Situação do negócio", "texto", e => e.Negocio.Status != null && e.Negocio.Status.TipoBase == TipoStatusNegocio.Ganho ? "Ganho" : e.Negocio.Status != null && e.Negocio.Status.TipoBase == TipoStatusNegocio.Perdido ? "Perdido" : "Aberto", null) { Rotulos = Dic(situacaoNegocio) });
        item.Dims.Add(new("CriadoEm", "Mês de criação", "mes", e => (e.CriadoEm.Year * 100 + e.CriadoEm.Month).ToString(), null));
        item.Filtros.Add(Cmp<ItemNegocio, long?>("Produto", "Produto", "id", e => e.ProdutoId, ref_: "Produto"));
        item.Filtros.Add(Cmp<ItemNegocio, long?>("Responsavel", "Responsável pelo negócio", "id", e => e.Negocio.ResponsavelId, ref_: "Usuario"));
        item.Filtros.Add(Cmp<ItemNegocio, long?>("Cliente", "Cliente do negócio", "id", e => (long?)e.Negocio.ClienteId, ref_: "Cliente"));
        item.Filtros.Add(Cmp<ItemNegocio, string?>("Situacao", "Situação do negócio", "enum", e => e.Negocio.Status != null && e.Negocio.Status.TipoBase == TipoStatusNegocio.Ganho ? "Ganho" : e.Negocio.Status != null && e.Negocio.Status.TipoBase == TipoStatusNegocio.Perdido ? "Perdido" : "Aberto", situacaoNegocio));
        item.Filtros.Add(Cmp<ItemNegocio, decimal?>("Total", "Total do item", "numero", e => (decimal?)e.Total));
        item.Filtros.Add(Cmp<ItemNegocio, DateTime?>("CriadoEm", "Data de criação", "data", e => e.CriadoEm));
        item.Periodos.Add("CriadoEm");
        dic[item.Chave] = item;

        return dic;
    }


    private static OpcaoRelDto O(string v, string r) => new() { Valor = v, Rotulo = r };
    private static Dictionary<string, string> Dic(IEnumerable<OpcaoRelDto> o) => o.ToDictionary(x => x.Valor, x => x.Rotulo);

    private static CampoRel<T> Cmp<T, TProp>(string chave, string rotulo, string tipo, Expression<Func<T, TProp>> sel,
        IEnumerable<OpcaoRelDto>? opcoes = null, string? ref_ = null)
        => new(chave, rotulo, tipo, sel, opcoes?.ToList(), ref_);

    /// <summary>Converte um enum (nulo ou não) em texto com o nome do membro, usando cadeia de comparações (traduzível pelo EF).</summary>
    private static Expression<Func<T, string?>> EnumTexto<T, TProp>(Expression<Func<T, TProp>> sel)
    {
        var p = sel.Parameters[0];
        var tipo = sel.Body.Type;
        var enumTipo = Nullable.GetUnderlyingType(tipo) ?? tipo;
        Expression resultado = Expression.Constant(null, typeof(string));
        foreach (var nome in Enum.GetNames(enumTipo).Reverse())
        {
            var valor = Expression.Constant(Enum.Parse(enumTipo, nome), tipo);
            resultado = Expression.Condition(Expression.Equal(sel.Body, valor), Expression.Constant(nome, typeof(string)), resultado);
        }
        return Expression.Lambda<Func<T, string?>>(resultado, p);
    }

    // ---------- Utilitários de expressão ----------
    internal sealed class Troca(ParameterExpression de, Expression para) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == de ? para : node;
    }

    internal static Expression Corpo(LambdaExpression l, ParameterExpression p) => new Troca(l.Parameters[0], p).Visit(l.Body)!;

    internal static string? TextoDe(JsonElement? v) => v is null ? null : v.Value.ValueKind switch
    {
        JsonValueKind.String => v.Value.GetString(),
        JsonValueKind.Number => v.Value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null
    };


    public sealed class Linha
    {
        public string? Chave { get; set; }
        public long? Moeda { get; set; }
        public decimal? Valor { get; set; }
    }

    public sealed record LinhaAgg(string? Chave, long? Moeda, int Qtd, decimal Soma, decimal? Media);

    // ---------- Tipos de definição ----------
    public sealed class ValRel<T>(string chave, string rotulo, bool monetario, Expression<Func<T, decimal?>> sel)
    {
        public string Chave { get; } = chave;
        public string Rotulo { get; } = rotulo;
        public bool Monetario { get; } = monetario;
        public Expression<Func<T, decimal?>> Sel { get; } = sel;
    }

    public sealed class DimRel<T>(string chave, string rotulo, string tipo, Expression<Func<T, string?>> sel, string? referencia)
    {
        public string Chave { get; } = chave;
        public string Rotulo { get; } = rotulo;
        public string Tipo { get; } = tipo;
        public Expression<Func<T, string?>> Sel { get; } = sel;
        public string? Referencia { get; } = referencia;
        public Dictionary<string, string>? Rotulos { get; init; }
    }

    public sealed class CampoRel<T>(string chave, string rotulo, string tipo, LambdaExpression sel, List<OpcaoRelDto>? opcoes, string? referencia)
    {
        public string Chave { get; } = chave;
        public string Rotulo { get; } = rotulo;
        public string Tipo { get; } = tipo;
        public LambdaExpression Sel { get; } = sel;
        public List<OpcaoRelDto>? Opcoes { get; } = opcoes;
        public string? Referencia { get; } = referencia;
    }

    public interface IEntidadeRel
    {
        string Chave { get; }
        string Rotulo { get; }
        EntidadeCatalogoDto Catalogo();
        void Validar(DefinicaoGraficoDto def);
        Task<RespostaDadosDto> ExecutarAsync(AppDbContext db, ConsultaDadosDto consulta, CancellationToken ct);
    }

    public sealed class EntidadeRel<T> : IEntidadeRel where T : class
    {
        private readonly Func<AppDbContext, IQueryable<T>> _fonte;
        private readonly Expression<Func<T, long?>>? _moeda;
        private readonly string _periodoPadrao;

        public EntidadeRel(string chave, string rotulo, Func<AppDbContext, IQueryable<T>> fonte, Expression<Func<T, long?>>? moeda, string periodoPadrao)
        {
            Chave = chave; Rotulo = rotulo; _fonte = fonte; _moeda = moeda; _periodoPadrao = periodoPadrao;
        }

        public string Chave { get; }
        public string Rotulo { get; }
        public List<ValRel<T>> Valores { get; } = [];
        public List<DimRel<T>> Dims { get; } = [];
        public List<CampoRel<T>> Filtros { get; } = [];
        public List<string> Periodos { get; } = [];

        public EntidadeCatalogoDto Catalogo()
        {
            var agregacoes = Valores.Count > 0 ? Agregacoes.ToList() : ["Contagem"];
            return new EntidadeCatalogoDto
            {
                Chave = Chave,
                Rotulo = Rotulo,
                Agregacoes = agregacoes,
                PeriodoCampoPadrao = _periodoPadrao,
                CamposValor = Valores.Select(v => new CampoCatalogoDto { Chave = v.Chave, Rotulo = v.Rotulo, Tipo = "numero", Monetario = v.Monetario }).ToList(),
                CamposAgrupamento = Dims.Select(d => new CampoCatalogoDto { Chave = d.Chave, Rotulo = d.Rotulo, Tipo = d.Tipo, Referencia = d.Referencia }).ToList(),
                CamposFiltro = Filtros.Select(f => new CampoCatalogoDto
                {
                    Chave = f.Chave, Rotulo = f.Rotulo, Tipo = f.Tipo, Referencia = f.Referencia,
                    Operadores = OperadoresPorTipo[f.Tipo].ToList(),
                    Opcoes = f.Opcoes
                }).ToList(),
                CamposPeriodo = Periodos.Select(c => { var f = Filtros.First(x => x.Chave == c); return new CampoCatalogoDto { Chave = c, Rotulo = f.Rotulo, Tipo = "data" }; }).ToList()
            };
        }

        // Valida a definição inteira sem consultar o banco.
        public void Validar(DefinicaoGraficoDto def) => Planejar(def);

        private sealed record Plano(string Agregacao, ValRel<T>? Valor, DimRel<T>? Dim, List<Expression<Func<T, bool>>> Predicados, CampoRel<T>? CampoPeriodo, int Limite);

        private Plano Planejar(DefinicaoGraficoDto def)
        {
            if (!Agregacoes.Contains(def.Agregacao)) throw new RegraNegocioException($"Agregação '{def.Agregacao}' inválida.");
            ValRel<T>? valor = null;
            if (def.Agregacao != "Contagem")
            {
                if (string.IsNullOrWhiteSpace(def.CampoValor))
                    throw new RegraNegocioException("Informe o campo de valor para soma ou média.");
                valor = Valores.FirstOrDefault(v => v.Chave == def.CampoValor)
                    ?? throw new RegraNegocioException($"Campo de valor '{def.CampoValor}' não está disponível para {Rotulo}.");
            }
            else if (!string.IsNullOrWhiteSpace(def.CampoValor) && Valores.All(v => v.Chave != def.CampoValor))
                throw new RegraNegocioException($"Campo de valor '{def.CampoValor}' não está disponível para {Rotulo}.");

            DimRel<T>? dim = null;
            if (!string.IsNullOrWhiteSpace(def.CampoAgrupamento))
                dim = Dims.FirstOrDefault(d => d.Chave == def.CampoAgrupamento)
                    ?? throw new RegraNegocioException($"Campo de agrupamento '{def.CampoAgrupamento}' não está disponível para {Rotulo}.");

            if (def.Filtros.Count > 10) throw new RegraNegocioException("No máximo 10 filtros por gráfico.");
            var preds = def.Filtros.Select(MontarFiltro).ToList();

            CampoRel<T>? campoPeriodo = null;
            if (!string.IsNullOrWhiteSpace(def.PeriodoCampo))
            {
                if (!Periodos.Contains(def.PeriodoCampo))
                    throw new RegraNegocioException($"Campo de período '{def.PeriodoCampo}' não está disponível para {Rotulo}.");
                campoPeriodo = Filtros.First(f => f.Chave == def.PeriodoCampo);
            }
            if (!string.IsNullOrWhiteSpace(def.PeriodoRelativo)) ResolverPeriodo(def.PeriodoRelativo);
            var limite = def.Limite ?? 30;
            if (limite is < 1 or > 100) throw new RegraNegocioException("O limite deve estar entre 1 e 100.");
            return new Plano(def.Agregacao, valor, dim, preds, campoPeriodo, limite);
        }

        private Expression<Func<T, bool>> MontarFiltro(FiltroRelatorioDto f)
        {
            var campo = Filtros.FirstOrDefault(c => c.Chave == f.Campo)
                ?? throw new RegraNegocioException($"Campo de filtro '{f.Campo}' não está disponível para {Rotulo}.");
            var op = (f.Operador ?? "").ToLowerInvariant();
            if (!OperadoresPorTipo[campo.Tipo].Contains(op))
                throw new RegraNegocioException($"Operador '{f.Operador}' não é permitido para o campo '{campo.Rotulo}'.");
            var p = Expression.Parameter(typeof(T), "e");
            var corpo = Corpo(campo.Sel, p);
            Expression teste;
            if (op is "nulo" or "naonulo")
            {
                if (Nullable.GetUnderlyingType(corpo.Type) is null && corpo.Type.IsValueType)
                    throw new RegraNegocioException($"Operador '{f.Operador}' não é permitido para o campo '{campo.Rotulo}'.");
                var nulo = Expression.Equal(corpo, Expression.Constant(null, corpo.Type));
                teste = op == "nulo" ? nulo : Expression.Not(nulo);
            }
            else
            {
                var texto = TextoDe(f.Valor);
                if (texto is null) throw new RegraNegocioException($"Informe um valor para o filtro '{campo.Rotulo}'.");
                var constante = Expression.Constant(Converter(campo, texto, corpo.Type), corpo.Type);
                teste = op switch
                {
                    "eq" => Expression.Equal(corpo, constante),
                    "ne" => Expression.NotEqual(corpo, constante),
                    "gt" => Expression.GreaterThan(corpo, constante),
                    "gte" => Expression.GreaterThanOrEqual(corpo, constante),
                    "lt" => Expression.LessThan(corpo, constante),
                    "lte" => Expression.LessThanOrEqual(corpo, constante),
                    "contem" => Expression.Call(corpo, typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!, constante),
                    _ => throw new RegraNegocioException($"Operador '{f.Operador}' inválido.")
                };
            }
            return Expression.Lambda<Func<T, bool>>(teste, p);
        }

        private static object Converter(CampoRel<T> campo, string texto, Type tipoCorpo)
        {
            var inv = CultureInfo.InvariantCulture;
            var erro = new RegraNegocioException($"Valor '{(texto.Length > 40 ? texto[..40] : texto)}' inválido para o campo '{campo.Rotulo}'.");
            switch (campo.Tipo)
            {
                case "id":
                    return long.TryParse(texto, NumberStyles.Integer, inv, out var l) ? l : throw erro;
                case "numero":
                    return decimal.TryParse(texto, NumberStyles.Number, inv, out var d) ? d : throw erro;
                case "data":
                    if (!DateTimeOffset.TryParse(texto, inv, DateTimeStyles.AssumeUniversal, out var dto)) throw erro;
                    return DateTime.SpecifyKind(dto.UtcDateTime, DateTimeKind.Utc);
                case "bool":
                    return bool.TryParse(texto, out var b) ? b : throw erro;
                case "texto":
                    if (texto.Length > 200) throw erro;
                    return texto;
                case "enum":
                    if (campo.Opcoes is null || campo.Opcoes.All(o => o.Valor != texto)) throw erro;
                    var alvo = Nullable.GetUnderlyingType(tipoCorpo) ?? tipoCorpo;
                    return alvo.IsEnum ? Enum.Parse(alvo, texto) : texto;
                default:
                    throw erro;
            }
        }

        private static Expression<Func<T, bool>> PredicadoPeriodo(CampoRel<T> campo, DateTime? ini, DateTime? fim)
        {
            var p = Expression.Parameter(typeof(T), "e");
            var corpo = Corpo(campo.Sel, p);
            Expression? teste = null;
            if (ini is not null)
                teste = Expression.GreaterThanOrEqual(corpo, Expression.Constant(ini.Value, corpo.Type));
            if (fim is not null)
            {
                var lt = Expression.LessThan(corpo, Expression.Constant(fim.Value, corpo.Type));
                teste = teste is null ? lt : Expression.AndAlso(teste, lt);
            }
            return Expression.Lambda<Func<T, bool>>(teste ?? Expression.Constant(true), p);
        }

        public async Task<RespostaDadosDto> ExecutarAsync(AppDbContext db, ConsultaDadosDto consulta, CancellationToken ct)
        {
            var plano = Planejar(consulta);
            // Período: o do painel sobrepõe o do gráfico; datas explícitas sobrepõem ambos.
            DateTime? ini = null, fim = null;
            var relativo = !string.IsNullOrWhiteSpace(consulta.PeriodoPainel) ? consulta.PeriodoPainel : consulta.PeriodoRelativo;
            if (!string.IsNullOrWhiteSpace(consulta.PeriodoPainel)) ResolverPeriodo(consulta.PeriodoPainel);
            if (consulta.PeriodoInicio is not null || consulta.PeriodoFim is not null)
            {
                ini = consulta.PeriodoInicio is null ? null : DateTime.SpecifyKind(consulta.PeriodoInicio.Value.ToUniversalTime(), DateTimeKind.Utc);
                fim = consulta.PeriodoFim is null ? null : DateTime.SpecifyKind(consulta.PeriodoFim.Value.ToUniversalTime(), DateTimeKind.Utc);
            }
            else if (ResolverPeriodo(relativo) is { } r) { ini = r.Inicio; fim = r.Fim; }

            var q = _fonte(db);
            foreach (var pr in plano.Predicados) q = q.Where(pr);
            if (ini is not null || fim is not null)
            {
                var campoP = plano.CampoPeriodo ?? Filtros.First(f => f.Chave == _periodoPadrao);
                q = q.Where(PredicadoPeriodo(campoP, ini, fim));
            }

            var usaValor = plano.Valor is not null;
            var monetario = usaValor && plano.Valor!.Monetario && _moeda is not null;

            // Projeção plana: Chave / Moeda / Valor
            var p = Expression.Parameter(typeof(T), "e");
            Expression chave = plano.Dim is null ? Expression.Constant(null, typeof(string)) : Corpo(plano.Dim.Sel, p);
            Expression moeda = monetario ? Corpo(_moeda!, p) : Expression.Constant(null, typeof(long?));
            Expression valor = usaValor ? Corpo(plano.Valor!.Sel, p) : Expression.Constant(0m, typeof(decimal?));
            var init = Expression.MemberInit(Expression.New(typeof(Linha)),
                Expression.Bind(typeof(Linha).GetProperty(nameof(Linha.Chave))!, chave),
                Expression.Bind(typeof(Linha).GetProperty(nameof(Linha.Moeda))!, moeda),
                Expression.Bind(typeof(Linha).GetProperty(nameof(Linha.Valor))!, valor));
            var proj = Expression.Lambda<Func<T, Linha>>(init, p);
            var linhas = q.Select(proj);

            List<LinhaAgg> agg;
            if (plano.Dim is null && !monetario)
            {
                var qtd = await linhas.CountAsync(ct);
                decimal soma = 0; decimal? media = null;
                if (usaValor && qtd > 0)
                {
                    soma = await linhas.SumAsync(l => l.Valor ?? 0, ct);
                    media = await linhas.AverageAsync(l => l.Valor, ct);
                }
                agg = [new LinhaAgg("total", null, qtd, soma, media)];
            }
            else if (plano.Dim is null)
            {
                agg = (await linhas.GroupBy(l => l.Moeda)
                    .Select(g => new { g.Key, Qtd = g.Count(), Soma = g.Sum(l => l.Valor), Media = g.Average(l => l.Valor) })
                    .ToListAsync(ct)).Select(x => new LinhaAgg("total", x.Key, x.Qtd, x.Soma ?? 0, x.Media)).ToList();
            }
            else if (!monetario)
            {
                agg = (await linhas.GroupBy(l => l.Chave)
                    .Select(g => new { g.Key, Qtd = g.Count(), Soma = g.Sum(l => l.Valor), Media = g.Average(l => l.Valor) })
                    .ToListAsync(ct)).Select(x => new LinhaAgg(x.Key, null, x.Qtd, x.Soma ?? 0, x.Media)).ToList();
            }
            else
            {
                agg = (await linhas.GroupBy(l => new { l.Chave, l.Moeda })
                    .Select(g => new { g.Key.Chave, g.Key.Moeda, Qtd = g.Count(), Soma = g.Sum(l => l.Valor), Media = g.Average(l => l.Valor) })
                    .ToListAsync(ct)).Select(x => new LinhaAgg(x.Chave, x.Moeda, x.Qtd, x.Soma ?? 0, x.Media)).ToList();
            }

            return await MontarRespostaAsync(db, plano, consulta, agg, monetario, ini, fim, ct);
        }

        private async Task<RespostaDadosDto> MontarRespostaAsync(AppDbContext db, Plano plano, ConsultaDadosDto consulta,
            List<LinhaAgg> agg, bool monetario, DateTime? ini, DateTime? fim, CancellationToken ct)
        {
            var dim = plano.Dim;
            var resposta = new RespostaDadosDto
            {
                Entidade = Chave, Agregacao = plano.Agregacao,
                CampoValor = plano.Valor?.Chave, CampoAgrupamento = dim?.Chave,
                TipoAgrupamento = dim?.Tipo, Monetario = monetario,
                PeriodoInicio = ini, PeriodoFim = fim
            };

            decimal Valor(LinhaAgg a) => Math.Round(plano.Agregacao switch
            {
                "Soma" => a.Soma,
                "Media" => a.Media ?? 0,
                _ => a.Qtd
            }, 2);

            // Rótulos de referências
            Dictionary<string, string> rotulosRef = [];
            if (dim?.Referencia is not null)
            {
                var ids = agg.Where(a => a.Chave is not null).Select(a => long.Parse(a.Chave!, CultureInfo.InvariantCulture)).Distinct().ToList();
                rotulosRef = await RelatorioReferencias.RotulosAsync(db, dim.Referencia, ids, ct);
            }
            string Rotulo(string? k)
            {
                if (dim is null) return "Total";
                if (k is null) return "(Sem valor)";
                if (dim.Tipo == "mes") return RotuloMes(k);
                if (dim.Rotulos is not null && dim.Rotulos.TryGetValue(k, out var r1)) return r1;
                if (dim.Referencia is not null) return rotulosRef.TryGetValue(k, out var r2) ? r2 : $"#{k}";
                return string.IsNullOrWhiteSpace(k) ? "(Sem valor)" : k;
            }

            var moedas = new Dictionary<long, MoedaSerieDto>();
            var idsMoeda = agg.Where(a => a.Moeda is not null).Select(a => a.Moeda!.Value).Distinct().ToList();
            if (idsMoeda.Count > 0)
                moedas = await db.Moedas.IgnoreQueryFilters().AsNoTracking().Where(m => m.Ativo && (m.EmpresaId == 0 || m.EmpresaId == db.EmpresaIdAtual) && idsMoeda.Contains(m.Id))
                    .Select(m => new MoedaSerieDto { Id = m.Id, Codigo = m.Codigo, Simbolo = m.Simbolo })
                    .ToDictionaryAsync(m => m.Id, ct);

            foreach (var grupo in agg.GroupBy(a => a.Moeda).OrderBy(g => g.Key is null ? 1 : 0).ThenBy(g => g.Key))
            {
                var serie = new SerieDadosDto();
                if (grupo.Key is { } mid)
                    serie.Moeda = moedas.TryGetValue(mid, out var m) ? m : new MoedaSerieDto { Id = mid, Codigo = $"#{mid}" };
                var itens = grupo.ToList();

                // Ordenação e limite
                if (dim?.Tipo == "mes")
                {
                    itens = itens.OrderBy(a => a.Chave is null ? 1 : 0).ThenBy(a => a.Chave, StringComparer.Ordinal).ToList();
                    itens = PreencherMeses(itens);
                    if (itens.Count > 60) { itens = itens.TakeLast(60).ToList(); resposta.Truncado = true; }
                }
                else if (dim is not null)
                {
                    itens = itens.OrderByDescending(Valor).ThenBy(a => a.Chave, StringComparer.Ordinal).ToList();
                    if (itens.Count > plano.Limite) { itens = itens.Take(plano.Limite).ToList(); resposta.Truncado = true; }
                }

                serie.Pontos = itens.Select(a => new PontoDadosDto
                {
                    Chave = a.Chave ?? "", Rotulo = Rotulo(a.Chave), Valor = Valor(a), Quantidade = a.Qtd
                }).ToList();
                serie.Quantidade = grupo.Sum(a => a.Qtd);
                serie.Total = plano.Agregacao switch
                {
                    "Contagem" => serie.Quantidade,
                    "Soma" => Math.Round(grupo.Sum(a => a.Soma), 2),
                    _ => dim is null ? Math.Round(grupo.First().Media ?? 0, 2) : null
                };
                resposta.Series.Add(serie);
            }

            if (resposta.Series.Count == 0)
                resposta.Series.Add(new SerieDadosDto { Total = plano.Agregacao == "Media" && dim is not null ? null : 0 });
            return resposta;
        }

        private static List<LinhaAgg> PreencherMeses(List<LinhaAgg> itens)
        {
            var comMes = itens.Where(a => a.Chave is { Length: 6 }).ToList();
            if (comMes.Count < 2) return itens;
            var moeda = comMes[0].Moeda;
            int Ano(string c) => int.Parse(c[..4], CultureInfo.InvariantCulture);
            int Mes(string c) => int.Parse(c[4..], CultureInfo.InvariantCulture);
            var existentes = comMes.ToDictionary(a => a.Chave!);
            var primeiro = comMes.First().Chave!; var ultimo = comMes.Last().Chave!;
            var resultado = new List<LinhaAgg>();
            var atual = new DateTime(Ano(primeiro), Mes(primeiro), 1);
            var limite = new DateTime(Ano(ultimo), Mes(ultimo), 1);
            var guarda = 0;
            while (atual <= limite && guarda++ < 120)
            {
                var k = (atual.Year * 100 + atual.Month).ToString(CultureInfo.InvariantCulture);
                resultado.Add(existentes.TryGetValue(k, out var a) ? a : new LinhaAgg(k, moeda, 0, 0, null));
                atual = atual.AddMonths(1);
            }
            resultado.AddRange(itens.Where(a => a.Chave is not { Length: 6 }));
            return resultado;
        }
    }
}

/// <summary>Referências (tabelas de apoio) usadas para rotular agrupamentos e popular filtros. Também é lista branca.</summary>
public static class RelatorioReferencias
{
    private sealed class Opc { public long Id { get; set; } public string Rotulo { get; set; } = ""; }

    private static readonly Dictionary<string, Func<AppDbContext, IQueryable<Opc>>> Mapa = new()
    {
        ["Funil"] = db => db.Funis.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["EtapaFunil"] = db => db.EtapasFunil.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Funil.Nome + " › " + x.Nome }),
        ["Usuario"] = db => db.Usuarios.AsNoTracking().Where(x => x.Tipo == TipoUsuario.Normal).Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Cliente"] = db => db.Clientes.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Moeda"] = db => db.Moedas.IgnoreQueryFilters().AsNoTracking().Where(x => x.Ativo && (x.EmpresaId == 0 || x.EmpresaId == db.EmpresaIdAtual)).Select(x => new Opc { Id = x.Id, Rotulo = x.Codigo }),
        ["StatusNegocio"] = db => db.StatusNegocios.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["MotivoPerda"] = db => db.MotivosPerda.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Produto"] = db => db.Produtos.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["StatusCliente"] = db => db.StatusClientes.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Origem"] = db => db.OrigensContato.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Ramo"] = db => db.RamosAtividade.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
        ["Porte"] = db => db.PortesEmpresa.AsNoTracking().Select(x => new Opc { Id = x.Id, Rotulo = x.Nome }),
    };

    public static async Task<Dictionary<string, string>> RotulosAsync(AppDbContext db, string referencia, List<long> ids, CancellationToken ct)
    {
        if (!Mapa.TryGetValue(referencia, out var fonte) || ids.Count == 0) return [];
        var lista = await fonte(db).Where(o => ids.Contains(o.Id)).ToListAsync(ct);
        return lista.ToDictionary(o => o.Id.ToString(CultureInfo.InvariantCulture), o => o.Rotulo);
    }

    public static async Task<List<OpcaoRelDto>> OpcoesAsync(AppDbContext db, string referencia, string? busca, CancellationToken ct)
    {
        if (!Mapa.TryGetValue(referencia, out var fonte))
            throw new RegraNegocioException($"Referência '{referencia}' não disponível.");
        var q = fonte(db);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            if (b.Length > 100) b = b[..100];
            q = q.Where(o => o.Rotulo.Contains(b));
        }
        var lista = await q.OrderBy(o => o.Rotulo).Take(200).ToListAsync(ct);
        return lista.Select(o => new OpcaoRelDto { Valor = o.Id.ToString(CultureInfo.InvariantCulture), Rotulo = o.Rotulo }).ToList();
    }
}

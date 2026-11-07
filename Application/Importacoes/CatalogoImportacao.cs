using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Importacoes;

public enum TipoDadoImportacao
{
    Texto, TextoLongo, Email, Telefone, Documento, Cep, Uf, Inteiro, Decimal, Data, Booleano, Categoria, Referencia
}

/// <summary>Campo que pode receber uma coluna do CSV. Lista branca: só o que está aqui é gravado.</summary>
/// <param name="Chave">Identificador estável usado no mapeamento (nome da propriedade nativa, ou "campo:{id}" nos personalizados).</param>
/// <param name="Tamanho">Tamanho máximo do texto (0 = sem limite).</param>
/// <param name="Sinonimos">Nomes alternativos de coluna usados na sugestão automática.</param>
public sealed record CampoImportavel(
    string Chave, string Rotulo, TipoDadoImportacao Tipo, bool Obrigatorio = false, int Tamanho = 0,
    string[]? Sinonimos = null, string? Dica = null,
    bool Personalizado = false, long? CampoId = null, string? CampoChaveInterna = null, TipoCampo? TipoPersonalizado = null)
{
    public string TipoNome => Tipo.ToString();
}

/// <summary>Entidades importáveis e seus campos nativos.</summary>
public static class CatalogoImportacao
{
    public static readonly string[] Entidades =
        [CatalogoEntidades.Cliente, CatalogoEntidades.PessoaContato, CatalogoEntidades.Produto, CatalogoEntidades.Negocio];

    public static string RotuloEntidade(string entidade) => entidade switch
    {
        CatalogoEntidades.Cliente => "Clientes",
        CatalogoEntidades.PessoaContato => "Contatos",
        CatalogoEntidades.Produto => "Produtos",
        CatalogoEntidades.Negocio => "Negócios",
        _ => entidade
    };

    /// <summary>Aceita a chave do catálogo ou o nome em português/plural, sem acento (cliente, clientes, contatos...).</summary>
    public static string? ResolverEntidade(string? texto)
    {
        var n = Normalizador.Chave(texto);
        return n switch
        {
            "cliente" or "clientes" => CatalogoEntidades.Cliente,
            "pessoacontato" or "pessoascontato" or "contato" or "contatos" => CatalogoEntidades.PessoaContato,
            "produto" or "produtos" => CatalogoEntidades.Produto,
            "negocio" or "negocios" => CatalogoEntidades.Negocio,
            _ => null
        };
    }

    public static EntidadeAlvo EntidadeAlvoDe(string entidade) => entidade switch
    {
        CatalogoEntidades.Cliente => EntidadeAlvo.Cliente,
        CatalogoEntidades.PessoaContato => EntidadeAlvo.PessoaContato,
        CatalogoEntidades.Produto => EntidadeAlvo.Produto,
        _ => EntidadeAlvo.Negocio
    };

    /// <summary>Chaves de duplicidade aceitas por entidade (valor enviado em "chave").</summary>
    public static string[] ChavesDuplicidade(string entidade) => entidade switch
    {
        CatalogoEntidades.Cliente => ["email", "documento", "nome"],
        CatalogoEntidades.PessoaContato => ["email", "documento", "nome"],
        CatalogoEntidades.Produto => ["codigo", "nome"],
        _ => ["nome"]
    };

    /// <summary>Campo (chave do mapeamento) que a chave de duplicidade exige estar mapeado.</summary>
    public static string CampoDaChave(string entidade, string chave) => chave switch
    {
        "email" => "Email",
        "documento" => "Documento",
        "codigo" => "Codigo",
        _ => entidade == CatalogoEntidades.Negocio ? "Titulo" : "Nome"
    };

    public static IReadOnlyList<CampoImportavel> CamposNativos(string entidade) => entidade switch
    {
        CatalogoEntidades.Cliente => ClienteCampos,
        CatalogoEntidades.PessoaContato => ContatoCampos,
        CatalogoEntidades.Produto => ProdutoCampos,
        CatalogoEntidades.Negocio => NegocioCampos,
        _ => []
    };

    private static readonly CampoImportavel[] ClienteCampos =
    [
        new("Nome", "Nome", TipoDadoImportacao.Texto, true, 200, ["nome fantasia", "empresa", "cliente", "name", "nome da empresa"]),
        new("RazaoSocial", "Razão social", TipoDadoImportacao.Texto, false, 300, ["razao social", "razao"]),
        new("Email", "E-mail", TipoDadoImportacao.Email, false, 150, ["email", "e-mail", "correio eletronico"]),
        new("Telefone", "Telefone", TipoDadoImportacao.Telefone, false, 30, ["fone", "celular", "phone", "whatsapp", "tel"]),
        new("Documento", "CNPJ/CPF", TipoDadoImportacao.Documento, false, 20, ["cnpj", "cpf", "documento", "cnpj/cpf", "cnpj cpf"], "11 dígitos (CPF) ou 14 (CNPJ)."),
        new("Cidade", "Cidade", TipoDadoImportacao.Texto, false, 100, ["municipio"]),
        new("Estado", "Estado (UF)", TipoDadoImportacao.Uf, false, 2, ["uf", "estado"], "Sigla com 2 letras."),
        new("Endereco", "Endereço", TipoDadoImportacao.Texto, false, 300, ["logradouro", "rua"]),
        new("Cep", "CEP", TipoDadoImportacao.Cep, false, 10, ["codigo postal"]),
        new("Site", "Site", TipoDadoImportacao.Texto, false, 300, ["website", "url", "pagina"]),
        new("Observacoes", "Observações", TipoDadoImportacao.TextoLongo, false, 0, ["obs", "notas", "anotacoes"]),
        new("StatusCliente", "Status", TipoDadoImportacao.Referencia, false, 0, ["status", "situacao"], "Nome de um status de cliente já cadastrado."),
        new("Origem", "Origem", TipoDadoImportacao.Referencia, false, 0, ["origem do contato", "fonte"], "Nome de uma origem já cadastrada."),
        new("RamoAtividade", "Ramo de atividade", TipoDadoImportacao.Referencia, false, 0, ["ramo", "segmento", "setor"], "Nome de um ramo já cadastrado."),
        new("PorteEmpresa", "Porte", TipoDadoImportacao.Referencia, false, 0, ["porte da empresa", "tamanho"], "Nome de um porte já cadastrado."),
    ];

    private static readonly CampoImportavel[] ContatoCampos =
    [
        new("Nome", "Nome", TipoDadoImportacao.Texto, true, 200, ["nome do contato", "contato", "pessoa", "name"]),
        new("Cliente", "Cliente (nome)", TipoDadoImportacao.Referencia, true, 0, ["empresa", "nome do cliente", "nome da empresa", "cliente"], "Nome de um cliente já cadastrado."),
        new("Email", "E-mail", TipoDadoImportacao.Email, false, 150, ["email", "e-mail"]),
        new("Telefone", "Telefone", TipoDadoImportacao.Telefone, false, 30, ["fone", "celular", "phone", "whatsapp", "tel"]),
        new("Cargo", "Cargo", TipoDadoImportacao.Texto, false, 100, ["funcao", "posicao", "job title"]),
        new("Documento", "CPF", TipoDadoImportacao.Documento, false, 20, ["cpf", "documento"]),
        new("Decisor", "Decisor", TipoDadoImportacao.Booleano, false, 0, ["tomador de decisao", "e decisor"], "Sim/Não."),
        new("Observacoes", "Observações", TipoDadoImportacao.TextoLongo, false, 0, ["obs", "notas"]),
    ];

    private static readonly CampoImportavel[] ProdutoCampos =
    [
        new("Nome", "Nome", TipoDadoImportacao.Texto, true, 200, ["produto", "descricao do produto", "name"]),
        new("Codigo", "Código", TipoDadoImportacao.Texto, false, 50, ["sku", "cod", "codigo do produto", "referencia"]),
        new("Descricao", "Descrição", TipoDadoImportacao.Texto, false, 1000, ["detalhes"]),
        new("Preco", "Preço", TipoDadoImportacao.Decimal, false, 0, ["valor", "preco de venda", "preco unitario", "price"], "Aceita 1.234,56 ou 1234.56."),
        new("PrecoCusto", "Preço de custo", TipoDadoImportacao.Decimal, false, 0, ["custo"]),
        new("Unidade", "Unidade", TipoDadoImportacao.Texto, false, 20, ["un", "unidade de medida"]),
        new("Categoria", "Categoria", TipoDadoImportacao.Categoria, false, 0, ["tipo"], "Produto, Serviço ou Assinatura."),
        new("Disponivel", "Disponível", TipoDadoImportacao.Booleano, false, 0, ["ativo", "disponibilidade"], "Sim/Não."),
        new("FamiliaProduto", "Família", TipoDadoImportacao.Referencia, false, 0, ["familia do produto"], "Nome de uma família já cadastrada."),
        new("GrupoProduto", "Grupo", TipoDadoImportacao.Referencia, false, 0, ["grupo do produto"], "Nome de um grupo já cadastrado."),
        new("Moeda", "Moeda (código)", TipoDadoImportacao.Referencia, false, 0, ["moeda", "currency", "codigo da moeda"], "Código, como BRL, USD ou EUR."),
    ];

    private static readonly CampoImportavel[] NegocioCampos =
    [
        new("Titulo", "Título", TipoDadoImportacao.Texto, true, 300, ["negocio", "nome do negocio", "nome", "titulo do negocio", "deal"]),
        new("Cliente", "Cliente (nome)", TipoDadoImportacao.Referencia, true, 0, ["empresa", "nome do cliente", "nome da empresa", "cliente"], "Nome de um cliente já cadastrado."),
        new("PessoaContato", "Contato (nome)", TipoDadoImportacao.Referencia, false, 0, ["contato", "nome do contato", "pessoa"], "Nome de um contato desse cliente."),
        new("Funil", "Funil", TipoDadoImportacao.Referencia, false, 0, ["pipeline", "nome do funil"], "Se vazio, usa o funil da etapa informada (ou o primeiro funil)."),
        new("Etapa", "Etapa", TipoDadoImportacao.Referencia, false, 0, ["fase", "estagio", "nome da etapa"], "Se vazia, usa a primeira etapa do funil."),
        new("Valor", "Valor", TipoDadoImportacao.Decimal, false, 0, ["valor do negocio", "total", "amount"], "Aceita 1.234,56 ou 1234.56."),
        new("Moeda", "Moeda (código)", TipoDadoImportacao.Referencia, false, 0, ["moeda", "currency", "codigo da moeda"], "Código, como BRL, USD ou EUR."),
        new("PrevisaoFechamento", "Previsão de fechamento", TipoDadoImportacao.Data, false, 0, ["previsao", "data prevista", "fechamento previsto", "data de fechamento"], "dd/mm/aaaa ou aaaa-mm-dd."),
        new("Responsavel", "Responsável", TipoDadoImportacao.Referencia, false, 0, ["dono", "vendedor", "responsavel pelo negocio"], "Nome ou e-mail de um usuário."),
    ];

    private static readonly TipoCampo[] TiposPersonalizadosSuportados =
    [
        TipoCampo.Texto, TipoCampo.TextoLongo, TipoCampo.Numero, TipoCampo.Decimal, TipoCampo.Moeda, TipoCampo.Data,
        TipoCampo.DataHora, TipoCampo.Booleano, TipoCampo.Lista, TipoCampo.Email, TipoCampo.Telefone, TipoCampo.Cpf,
        TipoCampo.Cnpj, TipoCampo.Cep, TipoCampo.Percentagem
    ];

    public static bool TipoPersonalizadoSuportado(TipoCampo tipo) => TiposPersonalizadosSuportados.Contains(tipo);

    public static TipoDadoImportacao TipoDadoDe(TipoCampo tipo) => tipo switch
    {
        TipoCampo.Numero => TipoDadoImportacao.Inteiro,
        TipoCampo.Decimal or TipoCampo.Moeda or TipoCampo.Percentagem => TipoDadoImportacao.Decimal,
        TipoCampo.Data or TipoCampo.DataHora => TipoDadoImportacao.Data,
        TipoCampo.Booleano => TipoDadoImportacao.Booleano,
        TipoCampo.Lista => TipoDadoImportacao.Categoria,
        TipoCampo.Email => TipoDadoImportacao.Email,
        TipoCampo.Telefone => TipoDadoImportacao.Telefone,
        TipoCampo.Cpf or TipoCampo.Cnpj => TipoDadoImportacao.Documento,
        TipoCampo.Cep => TipoDadoImportacao.Cep,
        TipoCampo.TextoLongo or TipoCampo.Html => TipoDadoImportacao.TextoLongo,
        _ => TipoDadoImportacao.Texto
    };

    /// <summary>Cabeçalho do modelo CSV: rótulos dos campos nativos.</summary>
    public static string[] CabecalhosModelo(string entidade) => CamposNativos(entidade).Select(c => c.Rotulo).ToArray();
}

/// <summary>Normalização de texto para comparação (sem acento, minúsculas, espaços colapsados).</summary>
public static class Normalizador
{
    public static string Texto(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        var espaco = false;
        foreach (var c in d)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(c)) { espaco = sb.Length > 0; continue; }
            if (espaco) { sb.Append(' '); espaco = false; }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Só letras e dígitos: "E-mail", "e mail" e "EMAIL" viram "email".</summary>
    public static string Chave(string? s)
    {
        var t = Texto(s);
        var sb = new StringBuilder(t.Length);
        foreach (var c in t) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    public static string SomenteDigitos(string? s)
        => s is null ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

    /// <summary>Coeficiente de Dice sobre bigramas (0 a 1).</summary>
    public static double Similaridade(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1;
        if (a.Length < 2 || b.Length < 2) return 0;
        var ba = new Dictionary<string, int>();
        for (var i = 0; i < a.Length - 1; i++)
        {
            var g = a.Substring(i, 2);
            ba[g] = ba.GetValueOrDefault(g) + 1;
        }
        var inter = 0;
        for (var i = 0; i < b.Length - 1; i++)
        {
            var g = b.Substring(i, 2);
            if (ba.TryGetValue(g, out var n) && n > 0) { inter++; ba[g] = n - 1; }
        }
        return 2.0 * inter / (a.Length - 1 + b.Length - 1);
    }
}

/// <summary>Conversão de texto de planilha (pt-BR) em tipos do domínio. Retorna false com a mensagem de erro.</summary>
public static partial class Conversores
{
    private static readonly HashSet<string> Ufs = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
    };

    [GeneratedRegex(@"^[^@\s,;]+@[^@\s,;]+\.[^@\s,;]{2,}$")]
    private static partial Regex RegexEmail();

    private static readonly string[] FormatosData =
    [
        "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "yyyy-MM-dd", "yyyy/MM/dd",
        "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss"
    ];

    public static bool Email(string v, out string valor, out string? erro)
    {
        valor = v.Trim();
        erro = null;
        if (!RegexEmail().IsMatch(valor)) { erro = "E-mail inválido."; return false; }
        return true;
    }

    public static bool Telefone(string v, out string valor, out string? erro)
    {
        valor = v.Trim();
        erro = null;
        if (valor.Any(c => !(char.IsDigit(c) || c is ' ' or '+' or '-' or '(' or ')' or '.' or '/')))
        { erro = "Telefone com caracteres inválidos (use só números, espaço, + - ( ) )."; return false; }
        var digitos = Normalizador.SomenteDigitos(valor).Length;
        if (digitos is < 8 or > 15) { erro = "Telefone deve ter entre 8 e 15 dígitos."; return false; }
        return true;
    }

    public static bool Documento(string v, out string valor, out string? erro)
    {
        valor = v.Trim();
        erro = null;
        if (valor.Any(c => !(char.IsDigit(c) || c is '.' or '-' or '/' or ' ')))
        { erro = "Documento deve conter só números e pontuação (. - /)."; return false; }
        var d = Normalizador.SomenteDigitos(valor).Length;
        if (d is not (11 or 14)) { erro = "Documento deve ter 11 dígitos (CPF) ou 14 (CNPJ)."; return false; }
        return true;
    }

    public static bool Cep(string v, out string valor, out string? erro)
    {
        valor = v.Trim();
        erro = null;
        if (Normalizador.SomenteDigitos(valor).Length != 8 || valor.Any(c => !(char.IsDigit(c) || c is '-' or '.' or ' ')))
        { erro = "CEP deve ter 8 dígitos."; return false; }
        return true;
    }

    public static bool Uf(string v, out string valor, out string? erro)
    {
        valor = v.Trim().ToUpperInvariant();
        erro = null;
        if (!Ufs.Contains(valor)) { erro = "UF inválida (use a sigla, como SP)."; return false; }
        return true;
    }

    public static bool Booleano(string v, out bool valor, out string? erro)
    {
        erro = null;
        switch (Normalizador.Chave(v))
        {
            case "sim" or "s" or "true" or "verdadeiro" or "1" or "yes" or "y" or "x" or "ativo":
                valor = true; return true;
            case "nao" or "n" or "false" or "falso" or "0" or "no" or "inativo":
                valor = false; return true;
            default:
                valor = false; erro = "Valor inválido (use Sim ou Não)."; return false;
        }
    }

    public static bool Inteiro(string v, out int valor, out string? erro)
    {
        erro = null;
        var t = v.Trim().Replace(".", "").Replace(" ", "");
        if (int.TryParse(t, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out valor)) return true;
        erro = "Número inteiro inválido.";
        return false;
    }

    /// <summary>1.234,56 · 1234,56 · 1234.56 · R$ 1.234,56 · 1,234.56. Rejeita texto e mais de 4 casas decimais não nulas.</summary>
    public static bool Decimal(string v, out decimal valor, out string? erro, bool permitirNegativo = false, int casas = 2)
    {
        erro = null;
        valor = 0;
        var t = v.Trim();
        t = Regex.Replace(t, @"^(R\$|US\$|\$|€|£)\s*", "", RegexOptions.IgnoreCase).Replace(" ", "");
        var negativo = false;
        if (t.StartsWith('-')) { negativo = true; t = t[1..]; }
        else if (t.StartsWith('+')) t = t[1..];
        if (t.Length == 0 || !t.All(c => char.IsDigit(c) || c is '.' or ','))
        { erro = "Número inválido (use algo como 1234,56 ou 1.234,56)."; return false; }

        var ultimoPonto = t.LastIndexOf('.');
        var ultimaVirgula = t.LastIndexOf(',');
        string normalizado;
        if (ultimoPonto >= 0 && ultimaVirgula >= 0)
        {
            var decimalSep = ultimoPonto > ultimaVirgula ? '.' : ',';
            var milharSep = decimalSep == '.' ? ',' : '.';
            if (t.Count(c => c == decimalSep) > 1) { erro = "Número inválido."; return false; }
            normalizado = t.Replace(milharSep.ToString(), "").Replace(decimalSep, '.');
        }
        else if (ultimaVirgula >= 0)
        {
            if (t.Count(c => c == ',') > 1) { erro = "Número inválido."; return false; }
            normalizado = t.Replace(',', '.');
        }
        else if (ultimoPonto >= 0)
        {
            var partes = t.Split('.');
            // "1.234" ou "1.234.567" é milhar no padrão brasileiro; "12.5" é decimal.
            var ehMilhar = partes.Length > 2 || (partes.Length == 2 && partes[1].Length == 3 && partes[0].Length is >= 1 and <= 3 && partes[0] != "0");
            if (partes.Length > 2 && partes.Skip(1).Any(p => p.Length != 3)) { erro = "Número inválido."; return false; }
            normalizado = ehMilhar ? string.Concat(partes) : t;
        }
        else normalizado = t;

        if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor))
        { erro = "Número inválido."; return false; }
        if (negativo) valor = -valor;
        if (valor < 0 && !permitirNegativo) { erro = "O valor não pode ser negativo."; return false; }
        if (Math.Abs(valor) >= 1_000_000_000_000m) { erro = "Número grande demais."; return false; }
        if (decimal.Round(valor, casas) != valor) { erro = $"Use no máximo {casas} casas decimais."; return false; }
        return true;
    }

    /// <summary>dd/mm/aaaa, d/m/aaaa, aaaa-mm-dd, ISO 8601 (com ou sem hora e fuso), com hora opcional. Devolve UTC.</summary>
    public static bool Data(string v, out DateTime valor, out string? erro)
    {
        erro = null;
        valor = default;
        var t = v.Trim();
        DateTime resultado;
        if (DateTime.TryParseExact(t, FormatosData, CultureInfo.InvariantCulture, DateTimeStyles.None, out resultado))
        {
            valor = DateTime.SpecifyKind(resultado, DateTimeKind.Utc);
        }
        else if (t.Contains('T') && DateTimeOffset.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            valor = dto.UtcDateTime;
        }
        else
        {
            erro = "Data inválida (use dd/mm/aaaa ou aaaa-mm-dd).";
            return false;
        }
        if (valor.Year is < 1900 or > 2200) { erro = "Data fora do intervalo aceito (1900 a 2200)."; return false; }
        return true;
    }
}

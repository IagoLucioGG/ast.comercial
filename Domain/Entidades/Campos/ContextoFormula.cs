using System.Reflection;

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Gera dinamicamente o schema de variáveis disponíveis para fórmulas JS.
/// Usa reflection para mapear propriedades e navegações das entidades.
/// O front consulta esse schema para autocompletar no editor de fórmulas.
/// </summary>
public static class ContextoFormula
{
    private const int ProfundidadeMaxima = 2;

    private static readonly HashSet<string> PropriedadesIgnoradas =
    [
        "Id", "EmpresaId", "CriadoEm", "AtualizadoEm", "Ativo", "OutrasPropriedades",
        "SenhaHash", "TokenIntegracao"
    ];

    private static readonly HashSet<Type> TiposIgnorados =
    [
        typeof(Campo), typeof(ConfiguracaoCampo), typeof(RegraCampo),
        typeof(OpcaoCampo), typeof(ValorCampo), typeof(TokenAcesso),
        typeof(MembroEquipe), typeof(RegistroAlteracao)
    ];

    private static readonly Dictionary<EntidadeAlvo, Type> MapaEntidades = new()
    {
        [EntidadeAlvo.Cliente] = typeof(Cliente),
        [EntidadeAlvo.PessoaContato] = typeof(PessoaContato),
        [EntidadeAlvo.Negocio] = typeof(Negocio),
        [EntidadeAlvo.Funil] = typeof(Funil),
        [EntidadeAlvo.EtapaFunil] = typeof(EtapaFunil),
        [EntidadeAlvo.Atividade] = typeof(Atividade),
        [EntidadeAlvo.Produto] = typeof(Produto),
        [EntidadeAlvo.Empresa] = typeof(Empresa),
        [EntidadeAlvo.Usuario] = typeof(Usuario),
        [EntidadeAlvo.Equipe] = typeof(Equipe),
        [EntidadeAlvo.Departamento] = typeof(Departamento),
        [EntidadeAlvo.Cargo] = typeof(Cargo),
        [EntidadeAlvo.Proposta] = typeof(Proposta),
        [EntidadeAlvo.ItemProposta] = typeof(ItemProposta),
        [EntidadeAlvo.SecaoProposta] = typeof(SecaoProposta),
        [EntidadeAlvo.ParcelaProposta] = typeof(ParcelaProposta)
    };

    public static Dictionary<string, EsquemaNavegacao> ObterEsquema(EntidadeAlvo entidade)
    {
        if (!MapaEntidades.TryGetValue(entidade, out var tipo))
            return new();

        var resultado = new Dictionary<string, EsquemaNavegacao>
        {
            ["$registro"] = GerarEsquema(tipo, 0),
            ["$campos"] = new EsquemaNavegacao { Tipo = "object", Descricao = "Campos personalizados (chave → valor)" }
        };

        // Só adiciona $usuario e $empresa como atalho se NÃO forem o próprio $registro
        if (tipo != typeof(Usuario))
            resultado["$usuario"] = GerarEsquema(typeof(Usuario), 0, maxNivel: 0);

        if (tipo != typeof(Empresa))
            resultado["$empresa"] = GerarEsquema(typeof(Empresa), 0, maxNivel: 0);

        // Adiciona navegações como atalhos, mas só as que NÃO são redundantes
        // com $usuario/$empresa e NÃO são o próprio tipo do $registro
        var navegacoes = ObterNavegacoes(tipo);
        foreach (var (nome, tipoNav) in navegacoes)
        {
            if (tipoNav == tipo) continue;
            var chave = $"${char.ToLower(nome[0])}{nome[1..]}";
            if (!resultado.ContainsKey(chave))
                resultado[chave] = GerarEsquema(tipoNav, 0, maxNivel: 1);
        }

        return resultado;
    }

    private static EsquemaNavegacao GerarEsquema(Type tipo, int nivelAtual, int? maxNivel = null, HashSet<Type>? visitados = null)
    {
        var limite = maxNivel ?? ProfundidadeMaxima;
        visitados ??= [];
        visitados.Add(tipo);

        var esquema = new EsquemaNavegacao
        {
            Tipo = tipo.Name,
            Propriedades = []
        };

        foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (PropriedadesIgnoradas.Contains(prop.Name)) continue;
            if (prop.Name.EndsWith("Id") && prop.Name != "Id") continue;

            var tipoProp = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (EhTipoSimples(tipoProp))
            {
                esquema.Propriedades[prop.Name] = new EsquemaNavegacao
                {
                    Tipo = ObterNomeTipo(tipoProp)
                };
            }
            else if (nivelAtual < limite && EhNavegacaoUnica(prop) && !TiposIgnorados.Contains(tipoProp))
            {
                // Evita recursão circular (ex: Departamento.DepartamentoPai.DepartamentoPai...)
                if (visitados.Contains(tipoProp)) continue;

                esquema.Propriedades[prop.Name] = GerarEsquema(tipoProp, nivelAtual + 1, maxNivel, [.. visitados]);
            }
        }

        return esquema;
    }

    private static List<(string Nome, Type Tipo)> ObterNavegacoes(Type tipo)
    {
        var resultado = new List<(string, Type)>();

        foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var tipoProp = prop.PropertyType;
            if (tipoProp.IsGenericType) continue;
            if (!tipoProp.IsClass || tipoProp == typeof(string)) continue;
            if (TiposIgnorados.Contains(tipoProp)) continue;
            if (tipoProp.IsAssignableTo(typeof(EntidadeBase)))
                resultado.Add((prop.Name, tipoProp));
        }

        return resultado;
    }

    private static bool EhTipoSimples(Type tipo) =>
        tipo == typeof(string) || tipo == typeof(int) || tipo == typeof(long) ||
        tipo == typeof(decimal) || tipo == typeof(double) || tipo == typeof(float) ||
        tipo == typeof(bool) || tipo == typeof(DateTime) || tipo.IsEnum;

    private static bool EhNavegacaoUnica(PropertyInfo prop)
    {
        var tipo = prop.PropertyType;
        if (tipo.IsGenericType) return false;
        return tipo.IsClass && tipo != typeof(string);
    }

    private static string ObterNomeTipo(Type tipo)
    {
        if (tipo == typeof(string)) return "string";
        if (tipo == typeof(int) || tipo == typeof(long)) return "number";
        if (tipo == typeof(decimal) || tipo == typeof(double) || tipo == typeof(float)) return "number";
        if (tipo == typeof(bool)) return "boolean";
        if (tipo == typeof(DateTime)) return "datetime";
        if (tipo.IsEnum) return "enum:" + string.Join("|", Enum.GetNames(tipo));
        return tipo.Name;
    }
}

public class EsquemaNavegacao
{
    public string Tipo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Chave { get; set; }
    public Dictionary<string, EsquemaNavegacao>? Propriedades { get; set; }
}

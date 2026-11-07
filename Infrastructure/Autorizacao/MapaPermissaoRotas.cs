using System.Text.RegularExpressions;
using AST.Comercial.Domain.Autorizacao;
using Microsoft.AspNetCore.Http;

namespace AST.Comercial.Infrastructure.Autorizacao;

public enum AcaoPermissao { Ver, Criar, Editar, Excluir }

public enum TipoRegra
{
    /// <summary>Qualquer usuário autenticado (ou rota não controlada pela matriz).</summary>
    Livre,
    /// <summary>Somente perfil Administrador.</summary>
    SomenteAdministrador,
    /// <summary>Exige a ação sobre a entidade (ou, na leitura, sobre alguma das alternativas).</summary>
    Entidade,
    /// <summary>Escrita de dados auxiliares de registros (valores de campos, etiquetas aplicadas): exige criar/editar em alguma entidade comercial de registro.</summary>
    EdicaoComercial,
    /// <summary>Leitura que exige Ver em qualquer entidade comercial (ex.: histórico de alterações).</summary>
    VerComercialQualquer,
    /// <summary>Rota de odata/* ou api/* que a matriz não conhece: só Administrador (falha fechada).</summary>
    NaoMapeada
}

public sealed record RegraRota(
    TipoRegra Tipo, AcaoPermissao Acao = AcaoPermissao.Ver, string? Entidade = null, string[]? LeituraAlternativa = null)
{
    public static readonly RegraRota Livre = new(TipoRegra.Livre);
}

/// <summary>
/// Converte (método HTTP, caminho) em uma regra de permissão. Pura, sem acesso a banco.
/// Mapa completo e exceções documentados em mapeamento/analise/11-permissoes-onda3.md.
/// </summary>
public static partial class MapaPermissaoRotas
{
    /// <param name="Entidade">Chave do catálogo.</param>
    /// <param name="LeituraAlt">Outras entidades cujo "Ver" também libera a leitura (dados que a tela de outra entidade precisa).</param>
    /// <param name="LeituraLivre">Leitura aberta a qualquer autenticado (dados de referência que formulários precisam).</param>
    /// <param name="Filho">Registro filho: qualquer escrita conta como "Editar" na entidade pai.</param>
    /// <param name="EscritaComercial">Escrita liberada a quem edita/cria alguma entidade comercial.</param>
    private sealed record Conjunto(
        string Entidade, string[]? LeituraAlt = null, bool LeituraLivre = false, bool Filho = false, bool EscritaComercial = false);

    private static readonly string[] AltNegocio = [CatalogoEntidades.Negocio];
    private static readonly string[] AltProposta = [CatalogoEntidades.Proposta];
    private static readonly string[] AltPreco = [CatalogoEntidades.Produto, CatalogoEntidades.Proposta, CatalogoEntidades.Negocio];

    /// <summary>EntitySets OData (e recursos api/*) → conjunto de regras. Chave em minúsculas.</summary>
    private static readonly Dictionary<string, Conjunto> Conjuntos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["clientes"] = new(CatalogoEntidades.Cliente),
        ["pessoascontato"] = new(CatalogoEntidades.PessoaContato),
        ["contatos"] = new(CatalogoEntidades.PessoaContato),
        ["negocios"] = new(CatalogoEntidades.Negocio),
        ["atividades"] = new(CatalogoEntidades.Atividade),
        ["propostas"] = new(CatalogoEntidades.Proposta),
        ["produtos"] = new(CatalogoEntidades.Produto),
        ["pedidos"] = new(CatalogoEntidades.Pedido),
        ["itenspedido"] = new(CatalogoEntidades.Pedido, Filho: true),
        ["etapaspedido"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["itensnegocio"] = new(CatalogoEntidades.Negocio, Filho: true),
        ["respostaschecklistnegocio"] = new(CatalogoEntidades.Negocio, Filho: true),
        ["itensproposta"] = new(CatalogoEntidades.Proposta, Filho: true),
        ["secoesproposta"] = new(CatalogoEntidades.Proposta, Filho: true),
        ["parcelasproposta"] = new(CatalogoEntidades.Proposta, Filho: true),
        ["listaspreco"] = new(CatalogoEntidades.ListaPreco, AltPreco),
        ["listas-preco"] = new(CatalogoEntidades.ListaPreco, AltPreco),
        ["itenslistapreco"] = new(CatalogoEntidades.ListaPreco, AltPreco, Filho: true),
        ["metas"] = new(CatalogoEntidades.Meta),
        ["paineisrelatorio"] = new(CatalogoEntidades.PainelRelatorio),
        ["graficosrelatorio"] = new(CatalogoEntidades.PainelRelatorio, Filho: true),
        ["relatorios"] = new(CatalogoEntidades.PainelRelatorio),
        ["paineis"] = new(CatalogoEntidades.PainelRelatorio),
        ["importacoes"] = new(CatalogoEntidades.Importacao),
        ["importacao"] = new(CatalogoEntidades.Importacao),
        ["etiquetas"] = new(CatalogoEntidades.Etiqueta),
        ["etiquetasregistro"] = new(CatalogoEntidades.Etiqueta, EscritaComercial: true),
        ["funis"] = new(CatalogoEntidades.Funil, AltNegocio),
        ["etapasfunil"] = new(CatalogoEntidades.Funil, AltNegocio, Filho: true),
        ["checklistsetapa"] = new(CatalogoEntidades.Funil, AltNegocio, Filho: true),
        ["itenschecklistetapa"] = new(CatalogoEntidades.Funil, AltNegocio, Filho: true),
        ["automacoes"] = new(CatalogoEntidades.Automacao),
        ["filtrosautomacao"] = new(CatalogoEntidades.Automacao, Filho: true),
        ["acoesautomacao"] = new(CatalogoEntidades.Automacao, Filho: true),
        ["execucoesautomacao"] = new(CatalogoEntidades.Automacao, Filho: true),
        ["filaautomacao"] = new(CatalogoEntidades.Automacao, Filho: true),
        ["integracoes"] = new(CatalogoEntidades.Integracao),
        ["logsintegracao"] = new(CatalogoEntidades.Integracao, Filho: true),
        ["usuarios"] = new(CatalogoEntidades.Usuario),
        ["equipes"] = new(CatalogoEntidades.Equipe),
        ["membrosequipe"] = new(CatalogoEntidades.Equipe, Filho: true),
        ["departamentos"] = new(CatalogoEntidades.Departamento),
        ["cargos"] = new(CatalogoEntidades.Cargo),
        ["campos"] = new(CatalogoEntidades.Campo, LeituraLivre: true),
        ["opcoescampo"] = new(CatalogoEntidades.Campo, LeituraLivre: true, Filho: true),
        ["regrascampo"] = new(CatalogoEntidades.Campo, LeituraLivre: true, Filho: true),
        ["configuracoescampo"] = new(CatalogoEntidades.Campo, LeituraLivre: true, Filho: true),
        ["valorescampo"] = new(CatalogoEntidades.Campo, LeituraLivre: true, EscritaComercial: true),
        ["formularios"] = new(CatalogoEntidades.Formulario, LeituraLivre: true),
        ["secoesformulario"] = new(CatalogoEntidades.Formulario, LeituraLivre: true, Filho: true),
        ["camposformulario"] = new(CatalogoEntidades.Formulario, LeituraLivre: true, Filho: true),
        ["modelosdocumento"] = new(CatalogoEntidades.ModeloDocumento, AltProposta),
        ["secoesmodelo"] = new(CatalogoEntidades.ModeloDocumento, AltProposta, Filho: true),
        ["fluxosaprovacao"] = new(CatalogoEntidades.FluxoAprovacao),
        ["fluxos-aprovacao"] = new(CatalogoEntidades.FluxoAprovacao),
        ["empresas"] = new(CatalogoEntidades.Empresa),
        ["statusnegocios"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["statusclientes"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["motivosperda"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["origenscontato"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["ramosatividade"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["portesempresa"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["familiasproduto"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["gruposproduto"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["moedas"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["localidades"] = new(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true),
        ["admincadastros"] = new(CatalogoEntidades.CadastroAuxiliar),
    };

    /// <summary>Rotas "Conjunto@Sub" que são cadastros auxiliares (leitura livre; escrita = CadastroAuxiliar com a ação do verbo).</summary>
    private static readonly HashSet<string> SubCadastrosAuxiliares = new(StringComparer.OrdinalIgnoreCase)
    {
        "clientes@origens", "clientes@status", "clientes@statusclientes", "clientes@ramosatividade", "clientes@portes",
        "negocios@status", "negocios@motivosperda",
        "produtos@familias", "produtos@grupos",
    };

    /// <summary>Entidades comerciais "de registro" usadas pela regra EdicaoComercial.</summary>
    public static readonly string[] EntidadesDeRegistro =
    [
        CatalogoEntidades.Cliente, CatalogoEntidades.PessoaContato, CatalogoEntidades.Negocio,
        CatalogoEntidades.Atividade, CatalogoEntidades.Proposta, CatalogoEntidades.Produto
    ];

    private static readonly HashSet<string> PostsDeLeitura = new(StringComparer.OrdinalIgnoreCase)
    {
        "consulta", "consultar", "dados", "executar", "preview", "previa", "agregar", "query"
    };

    private static readonly HashSet<string> CamposUsuarioPublicos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Nome", "Ativo", "FotoUrl"
    };

    private static readonly string[] CamposUsuarioProibidosNoFiltro = ["email", "perfil", "tipo", "token", "senha", "ultimoacesso"];

    [GeneratedRegex(@"\((\d+)\)")]
    private static partial Regex ChaveNumerica();

    public static RegraRota Resolver(string metodo, PathString caminho, IQueryCollection query, long? usuarioId)
    {
        var seg = (caminho.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (seg.Length == 0) return RegraRota.Livre;

        if (seg[0].Equals("odata", StringComparison.OrdinalIgnoreCase)) return ResolverOData(metodo, seg, query, usuarioId);
        if (seg[0].Equals("api", StringComparison.OrdinalIgnoreCase)) return ResolverApi(metodo, seg);
        return RegraRota.Livre; // fora de odata/* e api/* a matriz não atua
    }

    private static AcaoPermissao? AcaoDoVerbo(string metodo) => metodo.ToUpperInvariant() switch
    {
        "GET" or "HEAD" => AcaoPermissao.Ver,
        "POST" => AcaoPermissao.Criar,
        "PUT" or "PATCH" or "MERGE" => AcaoPermissao.Editar,
        "DELETE" => AcaoPermissao.Excluir,
        _ => null
    };

    // ---------------------------------------------------------------- OData
    private static RegraRota ResolverOData(string metodo, string[] seg, IQueryCollection query, long? usuarioId)
    {
        if (metodo.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase)) return RegraRota.Livre;
        if (seg.Length == 1) return RegraRota.Livre; // /odata (índice)

        var primeiro = seg[1];
        if (primeiro.StartsWith('$'))
            return primeiro.Equals("$metadata", StringComparison.OrdinalIgnoreCase) ? RegraRota.Livre : new RegraRota(TipoRegra.NaoMapeada);

        var posParen = primeiro.IndexOf('(');
        var nomeCompleto = posParen >= 0 ? primeiro[..posParen] : primeiro;
        var posArroba = nomeCompleto.IndexOf('@');
        var conjuntoNome = posArroba >= 0 ? nomeCompleto[..posArroba] : nomeCompleto;
        var sub = posArroba >= 0 ? nomeCompleto[(posArroba + 1)..] : null;
        var extras = seg.Skip(2).ToArray();

        var acaoBase = AcaoDoVerbo(metodo);
        if (acaoBase is null) return new RegraRota(TipoRegra.NaoMapeada);
        var acao = acaoBase.Value;

        // POST em ação/navegação depois da chave (Negocios(1)/Ganhar, Funis(2)/Etapas): é edição do registro,
        // exceto Renderizar (somente leitura, aceita POST ou GET).
        var ehPostDeAcao = acao == AcaoPermissao.Criar && extras.Length > 0;
        if (ehPostDeAcao)
            acao = extras[0].StartsWith("Renderizar", StringComparison.OrdinalIgnoreCase) ? AcaoPermissao.Ver : AcaoPermissao.Editar;

        // Visualizações salvas: preferências do próprio usuário.
        if (conjuntoNome.Equals("Visualizacoes", StringComparison.OrdinalIgnoreCase)) return RegraRota.Livre;
        // Solicitações de aprovação: o serviço confere se o usuário é o aprovador do nível.
        if (conjuntoNome.Equals("SolicitacoesAprovacao", StringComparison.OrdinalIgnoreCase)) return RegraRota.Livre;
        if (conjuntoNome.Equals("FluxosAprovacao", StringComparison.OrdinalIgnoreCase) && sub is not null &&
            (sub.Equals("Solicitacoes", StringComparison.OrdinalIgnoreCase)))
            return RegraRota.Livre;
        if (conjuntoNome.Equals("PermissoesPerfil", StringComparison.OrdinalIgnoreCase)) return new RegraRota(TipoRegra.SomenteAdministrador);
        if (conjuntoNome.Equals("RegistrosAlteracao", StringComparison.OrdinalIgnoreCase))
            return acao == AcaoPermissao.Ver ? new RegraRota(TipoRegra.VerComercialQualquer) : new RegraRota(TipoRegra.NaoMapeada);

        if (!Conjuntos.TryGetValue(conjuntoNome, out var conjunto))
            return new RegraRota(TipoRegra.NaoMapeada);

        // Usuários: lista de seleção (só Id/Nome/Ativo/FotoUrl) é aberta; trocar a própria senha também.
        if (conjuntoNome.Equals("Usuarios", StringComparison.OrdinalIgnoreCase))
        {
            if (acao == AcaoPermissao.Ver && sub is null && extras.Length == 0 && EhListaDeSelecaoDeUsuarios(query))
                return RegraRota.Livre;
            if (ehPostDeAcao && extras[0].Equals("AlterarSenha", StringComparison.OrdinalIgnoreCase) &&
                usuarioId is not null && ChaveDe(primeiro) == usuarioId)
                return RegraRota.Livre;
        }

        var ehAuxiliar = sub is not null && SubCadastrosAuxiliares.Contains($"{conjuntoNome}@{sub}");
        if (conjuntoNome.Equals("Campos", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(sub, "Valores", StringComparison.OrdinalIgnoreCase))
            conjunto = Conjuntos["valorescampo"]; // valores de campos personalizados gravam-se junto com o registro
        else if (ehAuxiliar)
            conjunto = new Conjunto(CatalogoEntidades.CadastroAuxiliar, LeituraLivre: true);
        else if (sub is not null)
            conjunto = conjunto with { Filho = true }; // Conjunto@Algo: dado filho/derivado do conjunto

        return Aplicar(acao, conjunto);
    }

    private static long? ChaveDe(string segmento)
    {
        var m = ChaveNumerica().Match(segmento);
        return m.Success && long.TryParse(m.Groups[1].Value, out var id) ? id : null;
    }

    /// <summary>
    /// $select só com Id, Nome, Ativo, FotoUrl, sem $expand/$apply e sem filtrar/ordenar por dado sensível.
    /// Existe porque as telas usam a lista de usuários para escolher o responsável.
    /// </summary>
    private static bool EhListaDeSelecaoDeUsuarios(IQueryCollection query)
    {
        if (!query.TryGetValue("$select", out var select) || string.IsNullOrWhiteSpace(select.ToString())) return false;
        if (query.ContainsKey("$expand") || query.ContainsKey("$apply")) return false;

        var campos = select.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (campos.Length == 0 || !campos.All(CamposUsuarioPublicos.Contains)) return false;

        foreach (var chave in new[] { "$filter", "$orderby" })
        {
            if (!query.TryGetValue(chave, out var valor)) continue;
            var texto = valor.ToString().ToLowerInvariant();
            if (CamposUsuarioProibidosNoFiltro.Any(texto.Contains)) return false;
        }
        return true;
    }

    private static RegraRota Aplicar(AcaoPermissao acao, Conjunto c)
    {
        if (acao == AcaoPermissao.Ver)
        {
            if (c.LeituraLivre) return RegraRota.Livre;
            return new RegraRota(TipoRegra.Entidade, AcaoPermissao.Ver, c.Entidade, c.LeituraAlt);
        }

        if (c.EscritaComercial) return new RegraRota(TipoRegra.EdicaoComercial, acao);
        if (c.Filho) acao = AcaoPermissao.Editar;
        return new RegraRota(TipoRegra.Entidade, acao, c.Entidade);
    }

    // ------------------------------------------------------------------ API
    private static RegraRota ResolverApi(string metodo, string[] seg)
    {
        if (metodo.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase)) return RegraRota.Livre;
        if (seg.Length < 2) return RegraRota.Livre;

        var recurso = seg[1].ToLowerInvariant();
        switch (recurso)
        {
            case "autenticacao": // login/refresh são anônimos; logout só precisa estar autenticado
            case "publico":      // anônimo
            case "permissoes":   // o controller decide (matriz e PUT = Administrador; catálogo e "minhas" = qualquer usuário)
            case "aprovacoes":   // caixa de aprovações: o serviço confere o aprovador
                return RegraRota.Livre;
            case "importar-modelo-ploomes":
                return new RegraRota(TipoRegra.SomenteAdministrador);
        }

        if (!Conjuntos.TryGetValue(recurso, out var conjunto))
            return new RegraRota(TipoRegra.NaoMapeada);

        var acaoBase = AcaoDoVerbo(metodo);
        if (acaoBase is null) return new RegraRota(TipoRegra.NaoMapeada);
        var acao = acaoBase.Value;
        var extras = seg.Skip(2).ToArray();

        if (acao == AcaoPermissao.Criar && extras.Length > 0)
        {
            // POST de consulta é leitura só em relatórios; em api/importacoes/executar grava dados (= Criar).
            if (PostsDeLeitura.Contains(extras[^1]) && conjunto.Entidade == CatalogoEntidades.PainelRelatorio) acao = AcaoPermissao.Ver;
            else if (extras.Any(e => long.TryParse(e, out _))) acao = AcaoPermissao.Editar; // api/x/{id}/ativar, .../copiar-precos-base
        }

        // api/negocios/{id}/checklist, api/produtos/{id}/precos, api/funis/{id}/checklist-progresso: subrecurso do registro
        if (extras.Length > 1 && long.TryParse(extras[0], out _) && acao != AcaoPermissao.Ver)
            acao = AcaoPermissao.Editar;

        return Aplicar(acao, conjunto);
    }
}

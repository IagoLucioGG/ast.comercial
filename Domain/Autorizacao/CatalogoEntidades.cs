namespace AST.Comercial.Domain.Autorizacao;

/// <summary>Entidade do sistema sobre a qual se define permissão por perfil.</summary>
/// <param name="Chave">Valor gravado em PermissaoPerfil.Entidade (estável, sem acento).</param>
/// <param name="Rotulo">Nome exibido ao usuário (pt-BR).</param>
/// <param name="Grupo">Comercial ou Administracao (usado pelo seed padrão e pela tela de permissões).</param>
public sealed record EntidadePermissionavel(string Chave, string Rotulo, string Grupo);

/// <summary>Catálogo estático das entidades que entram na matriz de permissões.</summary>
public static class CatalogoEntidades
{
    public const string GrupoComercial = "Comercial";
    public const string GrupoAdministracao = "Administracao";

    public const string Cliente = "Cliente";
    public const string PessoaContato = "PessoaContato";
    public const string Negocio = "Negocio";
    public const string Atividade = "Atividade";
    public const string Proposta = "Proposta";
    public const string Produto = "Produto";
    public const string ListaPreco = "ListaPreco";
    public const string Meta = "Meta";
    public const string PainelRelatorio = "PainelRelatorio";
    public const string Importacao = "Importacao";
    public const string Etiqueta = "Etiqueta";
    public const string Funil = "Funil";
    public const string Automacao = "Automacao";
    public const string Integracao = "Integracao";
    public const string Usuario = "Usuario";
    public const string Equipe = "Equipe";
    public const string Departamento = "Departamento";
    public const string Cargo = "Cargo";
    public const string Campo = "Campo";
    public const string Formulario = "Formulario";
    public const string ModeloDocumento = "ModeloDocumento";
    public const string FluxoAprovacao = "FluxoAprovacao";
    public const string Empresa = "Empresa";
    public const string CadastroAuxiliar = "CadastroAuxiliar";

    public static readonly IReadOnlyList<EntidadePermissionavel> Todas =
    [
        new(Cliente, "Clientes", GrupoComercial),
        new(PessoaContato, "Contatos", GrupoComercial),
        new(Negocio, "Negócios", GrupoComercial),
        new(Atividade, "Atividades", GrupoComercial),
        new(Proposta, "Propostas", GrupoComercial),
        new(Produto, "Produtos", GrupoComercial),
        new(ListaPreco, "Listas de preço", GrupoComercial),
        new(Meta, "Metas", GrupoComercial),
        new(PainelRelatorio, "Relatórios", GrupoComercial),
        new(Importacao, "Importações", GrupoComercial),
        new(Etiqueta, "Etiquetas", GrupoComercial),
        new(Funil, "Funis", GrupoAdministracao),
        new(Automacao, "Automações", GrupoAdministracao),
        new(Integracao, "Integrações", GrupoAdministracao),
        new(Usuario, "Usuários", GrupoAdministracao),
        new(Equipe, "Equipes", GrupoAdministracao),
        new(Departamento, "Departamentos", GrupoAdministracao),
        new(Cargo, "Cargos", GrupoAdministracao),
        new(Campo, "Campos personalizados", GrupoAdministracao),
        new(Formulario, "Formulários", GrupoAdministracao),
        new(ModeloDocumento, "Modelos de documento", GrupoAdministracao),
        new(FluxoAprovacao, "Fluxos de aprovação", GrupoAdministracao),
        new(Empresa, "Empresa", GrupoAdministracao),
        new(CadastroAuxiliar, "Cadastros auxiliares", GrupoAdministracao),
    ];

    public static bool Existe(string chave) => Todas.Any(e => e.Chave == chave);
}

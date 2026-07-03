namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Visualização salva (aba com filtros) — genérica para qualquer entidade.
/// Funciona como "views" personalizadas: cada usuário ou empresa pode criar abas
/// com filtros pré-aplicados, colunas visíveis e ordenação.
/// </summary>
public class VisualizacaoSalva : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public EntidadeAlvo EntidadeAlvo { get; set; }

    /// <summary>
    /// Expressão OData $filter salva (ex: "Perfil eq 'Administrador' and Ativo eq true")
    /// </summary>
    public string? Filtro { get; set; }

    /// <summary>
    /// Colunas visíveis separadas por vírgula (ex: "Nome,Email,Perfil,UltimoAcesso")
    /// </summary>
    public string? Colunas { get; set; }

    /// <summary>
    /// Expressão OData $orderby (ex: "Nome asc")
    /// </summary>
    public string? Ordenacao { get; set; }

    /// <summary>
    /// Itens por página desta visualização
    /// </summary>
    public int ItensPorPagina { get; set; } = 20;

    /// <summary>
    /// Visibilidade: null = todos da empresa, preenchido = apenas esses usuários (IDs separados por vírgula)
    /// </summary>
    public string? UsuariosVisiveis { get; set; }

    /// <summary>
    /// Usuário que criou a visualização
    /// </summary>
    public long? CriadoPorId { get; set; }

    /// <summary>
    /// Ordem de exibição nas abas
    /// </summary>
    public int Ordem { get; set; }

    /// <summary>
    /// Se é a aba padrão da entidade (todos os registros, sem filtro)
    /// </summary>
    public bool Padrao { get; set; }
}

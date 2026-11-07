namespace AST.Comercial.Domain.Entidades;

// ===== Permissões =====

public enum EscopoVisibilidade
{
    Todos = 0,
    Equipe = 1,
    Proprios = 2
}

/// <summary>Permissão de um perfil sobre uma entidade do sistema (ver CatalogoEntidades).</summary>
public class PermissaoPerfil : EntidadeBase
{
    public PerfilUsuario Perfil { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public bool PodeVer { get; set; }
    public bool PodeCriar { get; set; }
    public bool PodeEditar { get; set; }
    public bool PodeExcluir { get; set; }
    public EscopoVisibilidade Escopo { get; set; } = EscopoVisibilidade.Todos;
}

// ===== Itens do negócio =====

public class ItemNegocio : EntidadeBase
{
    public long NegocioId { get; set; }
    public Negocio Negocio { get; set; } = null!;

    public long? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; } = 1;
    public decimal PrecoUnitario { get; set; }
    /// <summary>Desconto em percentual (0 a 100).</summary>
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public int Ordem { get; set; }
}

// ===== Listas de preço =====

public class ListaPreco : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public long? MoedaId { get; set; }
    public Moeda? Moeda { get; set; }

    public bool Padrao { get; set; }
    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }

    public ICollection<ItemListaPreco> Itens { get; set; } = [];
}

public class ItemListaPreco : EntidadeBase
{
    public long ListaPrecoId { get; set; }
    public ListaPreco ListaPreco { get; set; } = null!;

    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public decimal Preco { get; set; }
}

// ===== Checklists por etapa =====

public class ChecklistEtapa : EntidadeBase
{
    public long EtapaFunilId { get; set; }
    public EtapaFunil EtapaFunil { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }

    public ICollection<ItemChecklistEtapa> Itens { get; set; } = [];
}

public class ItemChecklistEtapa : EntidadeBase
{
    public long ChecklistEtapaId { get; set; }
    public ChecklistEtapa ChecklistEtapa { get; set; } = null!;

    public string Texto { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
    public int Ordem { get; set; }
}

public class RespostaChecklistNegocio : EntidadeBase
{
    public long NegocioId { get; set; }
    public Negocio Negocio { get; set; } = null!;

    public long ItemChecklistEtapaId { get; set; }
    public ItemChecklistEtapa ItemChecklistEtapa { get; set; } = null!;

    public bool Concluido { get; set; }

    public long? ConcluidoPorUsuarioId { get; set; }
    public Usuario? ConcluidoPorUsuario { get; set; }
    public DateTime? ConcluidoEm { get; set; }
}

// ===== Metas =====

public enum MetricaMeta
{
    ValorGanho = 0,
    QuantidadeGanhos = 1,
    NegociosCriados = 2,
    AtividadesConcluidas = 3,
    PropostasAprovadas = 4
}

public class Meta : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public MetricaMeta Metrica { get; set; }
    public decimal Alvo { get; set; }

    public long? MoedaId { get; set; }
    public Moeda? Moeda { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public long? EquipeId { get; set; }
    public Equipe? Equipe { get; set; }

    public long? FunilId { get; set; }
    public Funil? Funil { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
}

// ===== Relatórios =====

public enum TipoGrafico
{
    Numero = 0,
    Barras = 1,
    Linhas = 2,
    Pizza = 3,
    Tabela = 4
}

public enum AgregacaoGrafico
{
    Contagem = 0,
    Soma = 1,
    Media = 2
}

public class PainelRelatorio : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Compartilhado { get; set; }

    public long? CriadoPorUsuarioId { get; set; }
    public Usuario? CriadoPorUsuario { get; set; }

    public int Ordem { get; set; }

    public ICollection<GraficoRelatorio> Graficos { get; set; } = [];
}

public class GraficoRelatorio : EntidadeBase
{
    public long PainelRelatorioId { get; set; }
    public PainelRelatorio PainelRelatorio { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public TipoGrafico Tipo { get; set; }
    /// <summary>Chave de CatalogoEntidades (ex.: Negocio).</summary>
    public string Entidade { get; set; } = string.Empty;
    public AgregacaoGrafico Agregacao { get; set; }
    public string? CampoValor { get; set; }
    public string? CampoAgrupamento { get; set; }
    public string? FiltroJson { get; set; }
    public string? PeriodoCampo { get; set; }
    /// <summary>Ex.: "7d", "30d", "mes", "ano".</summary>
    public string? PeriodoRelativo { get; set; }
    /// <summary>Largura em colunas da grade (1 a 4).</summary>
    public int Largura { get; set; } = 2;
    public int Ordem { get; set; }
}

// ===== Importação =====

public enum StatusImportacao
{
    Pendente = 0,
    Processando = 1,
    Concluida = 2,
    ConcluidaComErros = 3,
    Falhou = 4
}

public class Importacao : EntidadeBase
{
    public string Entidade { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public StatusImportacao Status { get; set; } = StatusImportacao.Pendente;
    public int TotalLinhas { get; set; }
    public int Sucesso { get; set; }
    public int Falhas { get; set; }
    public string MapeamentoJson { get; set; } = "{}";
    public string ErrosJson { get; set; } = "[]";

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime? ConcluidaEm { get; set; }
}

namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Fluxo de aprovação — define condições que disparam alerta, bloqueio ou controle de alçada.
/// </summary>
public class FluxoAprovacao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public EntidadeAlvo EntidadeAlvo { get; set; }
    public TipoFluxoAprovacao Tipo { get; set; }
    public string? Mensagem { get; set; }
    public int Ordem { get; set; }

    public ICollection<FiltroFluxoAprovacao> Filtros { get; set; } = [];
    public ICollection<NivelAprovacao> Niveis { get; set; } = [];
}

public enum TipoFluxoAprovacao
{
    Alerta = 0,
    Bloqueio = 1,
    ControleAlcada = 2
}

/// <summary>
/// Filtro/Condição do fluxo (mesmo modelo: E/OU por grupo)
/// </summary>
public class FiltroFluxoAprovacao : EntidadeBase
{
    public long FluxoAprovacaoId { get; set; }
    public FluxoAprovacao FluxoAprovacao { get; set; } = null!;

    public int Grupo { get; set; }
    public int Ordem { get; set; }
    public string CampoReferencia { get; set; } = string.Empty;
    public OperadorFiltro Operador { get; set; }
    public string? Valor { get; set; }
}

/// <summary>
/// Nível de aprovação (todos precisam ser aprovados em sequência)
/// </summary>
public class NivelAprovacao : EntidadeBase
{
    public long FluxoAprovacaoId { get; set; }
    public FluxoAprovacao FluxoAprovacao { get; set; } = null!;

    public int Nivel { get; set; }
    public string? Descricao { get; set; }

    public ICollection<AprovadorNivel> Aprovadores { get; set; } = [];
}

/// <summary>
/// Aprovador: pode ser usuário específico OU qualquer um com determinado perfil
/// </summary>
public class AprovadorNivel : EntidadeBase
{
    public long NivelAprovacaoId { get; set; }
    public NivelAprovacao NivelAprovacao { get; set; } = null!;

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public PerfilUsuario? Perfil { get; set; }
}

/// <summary>
/// Solicitação de aprovação pendente/concluída para um registro específico
/// </summary>
public class SolicitacaoAprovacao : EntidadeBase
{
    public long FluxoAprovacaoId { get; set; }
    public FluxoAprovacao FluxoAprovacao { get; set; } = null!;

    public long RegistroId { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }

    public int NivelAtual { get; set; } = 1;
    public StatusSolicitacaoAprovacao Status { get; set; } = StatusSolicitacaoAprovacao.Pendente;

    public long? AprovadoPorId { get; set; }
    public Usuario? AprovadoPor { get; set; }
    public DateTime? AprovadoEm { get; set; }
    public DateTime? RejeitadoEm { get; set; }
    public string? Observacao { get; set; }
}

public enum StatusSolicitacaoAprovacao
{
    Pendente = 0,
    AprovadoNivel = 1,
    Aprovado = 2,
    Rejeitado = 3
}

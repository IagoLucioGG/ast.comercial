namespace AST.Comercial.Application.Dtos;

// ---------- Fluxos (cadastro) ----------

public class FiltroFluxoAprovacaoDto
{
    public int Grupo { get; set; }
    public string CampoReferencia { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;
    public string? Valor { get; set; }
}

public class AprovadorNivelDto
{
    public long? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    /// <summary>Administrador, Gerente, Vendedor ou Visualizador (qualquer usuário com o perfil aprova).</summary>
    public string? Perfil { get; set; }
}

public class NivelFluxoAprovacaoDto
{
    public int Nivel { get; set; }
    public string? Descricao { get; set; }
    public List<AprovadorNivelDto> Aprovadores { get; set; } = [];
}

public class FluxoAprovacaoDto
{
    public long? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    /// <summary>Alerta | Bloqueio | ControleAlcada. Alerta nunca gera solicitação.</summary>
    public string Tipo { get; set; } = "ControleAlcada";
    public string? Mensagem { get; set; }
    public int Ordem { get; set; }
    /// <summary>Nulo na entrada: cria como ativo / mantém o valor atual.</summary>
    public bool? Ativo { get; set; }
    public List<FiltroFluxoAprovacaoDto> Filtros { get; set; } = [];
    public List<NivelFluxoAprovacaoDto> Niveis { get; set; } = [];
}

public record CampoFiltroAprovacaoDto(string Campo, string Rotulo, string Tipo, string[] Operadores, string Ajuda);

// ---------- Solicitações ----------

public class PropostaResumoAprovacaoDto
{
    public long Id { get; set; }
    public int? Numero { get; set; }
    public string? Titulo { get; set; }
    public long? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public decimal? Valor { get; set; }
    public long? MoedaId { get; set; }
    public string? MoedaSimbolo { get; set; }
    public string? MoedaCodigo { get; set; }
    /// <summary>Desconto total efetivo (itens + desconto da proposta) em % do valor bruto.</summary>
    public decimal DescontoPercentual { get; set; }
    public string? StatusAprovacao { get; set; }
}

public class PassoAprovacaoDto
{
    public int Nivel { get; set; }
    public string? Descricao { get; set; }
    public List<string> Aprovadores { get; set; } = [];
    /// <summary>Aprovado | Rejeitado | Atual | Aguardando | Cancelado</summary>
    public string Estado { get; set; } = "Aguardando";
    public long? DecididoPorId { get; set; }
    public string? DecididoPorNome { get; set; }
    public DateTime? DecididoEm { get; set; }
    public string? Comentario { get; set; }
    /// <summary>Administrador que decidiu sem ser aprovador do nível.</summary>
    public bool ForaDaAlcada { get; set; }
}

public class SolicitacaoAprovacaoDto
{
    public long Id { get; set; }
    public long FluxoId { get; set; }
    public string? FluxoNome { get; set; }
    public string Entidade { get; set; } = "Proposta";
    public long RegistroId { get; set; }
    /// <summary>Status gravado: Pendente | AprovadoNivel | Aprovado | Rejeitado.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>EmAndamento | Aprovada | Rejeitada | Cancelada</summary>
    public string Situacao { get; set; } = string.Empty;
    public int NivelAtual { get; set; }
    public int TotalNiveis { get; set; }
    public long? SolicitadoPorId { get; set; }
    public string? SolicitadoPorNome { get; set; }
    public DateTime SolicitadoEm { get; set; }
    public string? Observacao { get; set; }
    public long? DecididoPorId { get; set; }
    public string? DecididoPorNome { get; set; }
    public DateTime? DecididoEm { get; set; }
    public string? ComentarioDecisao { get; set; }
    /// <summary>O usuário logado pode aprovar/rejeitar agora (aprovador do nível atual ou Administrador).</summary>
    public bool PodeDecidir { get; set; }
    public PropostaResumoAprovacaoDto? Proposta { get; set; }
    public List<PassoAprovacaoDto> Passos { get; set; } = [];
}

public record DecisaoAprovacaoRequest(string? Comentario, string? Observacao);
public record SolicitarAprovacaoRequest(string? Observacao);

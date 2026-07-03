namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Ação executada quando a automação dispara e os filtros são verdadeiros.
/// Configuração armazenada como JSON — o motor de execução interpreta por TipoAcao.
/// </summary>
public class AcaoAutomacao : EntidadeBase
{
    public long AutomacaoId { get; set; }
    public Automacao Automacao { get; set; } = null!;

    public TipoAcao Tipo { get; set; }
    public int Ordem { get; set; }

    /// <summary>
    /// Configuração da ação em JSON.
    /// Cada TipoAcao tem seu schema de configuração.
    /// 
    /// EditarDados: { "alteracoes": [{ "campo": "Status", "valor": "Ganho" }] }
    /// EnviarEmail: { "destinatario": "$contato.Email", "assunto": "...", "corpo": "...", "templateId": 5 }
    /// CriarTarefa: { "titulo": "Follow-up", "tipo": "Ligacao", "prazo": "+3d", "responsavelId": null }
    /// CriarContato: { "nome": "$registro.PessoaContato", "email": "$registro.EmailContato" }
    /// MoverEtapa: { "etapaId": 15 }
    /// AlterarStatus: { "statusId": 3 }
    /// AdicionarEtiqueta: { "etiquetaId": 7 }
    /// RemoverEtiqueta: { "etiquetaId": 7 }
    /// NotificarUsuario: { "usuarioId": null, "mensagem": "Negócio ganho: $registro.Titulo" }
    /// CriarProposta: { "templateId": 10 }
    /// CriarRegistroInteracao: { "tipo": "Nota", "descricao": "Automação executada" }
    /// ExecutarWebhook: { "url": "https://...", "metodo": "POST", "headers": {} }
    /// </summary>
    public string Configuracao { get; set; } = "{}";
}

/// <summary>
/// Tipos de ação disponíveis no motor de automação
/// </summary>
public enum TipoAcao
{
    EditarDados = 0,
    EnviarEmail = 1,
    CriarTarefa = 2,
    CriarContato = 3,
    CriarRegistroInteracao = 4,
    AlterarStatus = 5,
    MoverEtapa = 6,
    AdicionarEtiqueta = 7,
    RemoverEtiqueta = 8,
    NotificarUsuario = 9,
    CriarProposta = 10,
    AlterarResponsavel = 11,
    CriarNegocio = 12,
    ConcluirTarefa = 13,
    GerarDocumento = 14,
    ExecutarWebhook = 15
}

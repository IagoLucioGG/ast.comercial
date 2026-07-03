namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Integração externa configurada. Configuração completa tipo Postman:
/// autenticação, headers, base URL, tipo de API.
/// </summary>
public class Integracao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Provedor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? LogoUrl { get; set; }
    public StatusIntegracao Status { get; set; } = StatusIntegracao.Desligado;

    // --- Conexão (tipo Postman) ---
    public string? ApiUrl { get; set; }
    public TipoAutenticacao TipoAuth { get; set; } = TipoAutenticacao.Bearer;
    public string? ApiToken { get; set; }
    public string? AuthUsuario { get; set; }
    public string? AuthSenha { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? OAuthTokenUrl { get; set; }
    public string? OAuthScope { get; set; }
    public string? ApiKeyHeader { get; set; }
    public string? ApiKeyValor { get; set; }
    public string? HeadersFixos { get; set; }
    public TipoApi TipoApi { get; set; } = TipoApi.Rest;

    // Webhook
    public string? WebhookSecret { get; set; }
    public string? ChaveWebhook { get; set; }

    public string? Configuracao { get; set; }
    public DateTime? UltimaSincronizacao { get; set; }

    public ICollection<MapeamentoCampoIntegracao> Mapeamentos { get; set; } = [];
    public ICollection<FluxoIntegracao> Fluxos { get; set; } = [];
}

public enum StatusIntegracao
{
    Desligado = 0,
    Ligado = 1,
    Erro = 2
}

public enum TipoAutenticacao
{
    Nenhuma = 0,
    Bearer = 1,
    BasicAuth = 2,
    ApiKey = 3,
    OAuth2ClientCredentials = 4
}

public enum TipoApi
{
    Rest = 0,
    OData = 1,
    GraphQL = 2,
    Soap = 3
}

/// <summary>
/// Mapeamento: campo interno ↔ campo externo.
/// Vinculado a um FluxoIntegracao específico (cada endpoint pode ter mapeamento diferente).
/// </summary>
public class MapeamentoCampoIntegracao : EntidadeBase
{
    public long IntegracaoId { get; set; }
    public Integracao Integracao { get; set; } = null!;

    /// <summary>
    /// Fluxo específico ao qual este mapeamento pertence (opcional)
    /// </summary>
    public long? FluxoIntegracaoId { get; set; }
    public FluxoIntegracao? FluxoIntegracao { get; set; }

    public EntidadeAlvo EntidadeAlvo { get; set; }
    public string CampoInterno { get; set; } = string.Empty;
    public string CampoExterno { get; set; } = string.Empty;
    public DirecaoMapeamento Direcao { get; set; } = DirecaoMapeamento.Ambos;

    /// <summary>
    /// Transformação (ex: "uppercase", "date:yyyy-MM-dd", fórmula JS)
    /// </summary>
    public string? Transformacao { get; set; }
    public string? ValorPadrao { get; set; }
    public bool Obrigatorio { get; set; }
    public int Ordem { get; set; }
}

public enum DirecaoMapeamento
{
    Entrada = 0,
    Saida = 1,
    Ambos = 2
}

/// <summary>
/// Fluxo de sincronização — define como dados fluem entre AST e sistema externo.
/// Totalmente configurável: suporta webhook, polling e envio por evento.
/// Se amanhã o Bimer liberar webhook pra uma entidade, basta mudar ModoEntrada de Polling para Webhook.
/// </summary>
public class FluxoIntegracao : EntidadeBase
{
    public long IntegracaoId { get; set; }
    public Integracao Integracao { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;
    public DirecaoFluxo Direcao { get; set; }
    public EntidadeAlvo EntidadeAlvo { get; set; }

    /// <summary>
    /// Modo de captura para ENTRADA: Webhook, Polling ou Manual
    /// </summary>
    public ModoEntrada? ModoEntrada { get; set; }

    // --- SAÍDA (AST → externo) ---
    public string? EventoGatilho { get; set; }
    public string? EndpointSaida { get; set; }
    public string? MetodoHttpSaida { get; set; }
    public string? TemplateBody { get; set; }

    // --- ENTRADA Webhook ---
    public string? WebhookCampoIdentificador { get; set; }
    public string? WebhookValorIdentificador { get; set; }

    // --- ENTRADA Polling ---
    public string? EndpointPolling { get; set; }
    public string? MetodoHttpPolling { get; set; }
    public int? IntervaloPollingMinutos { get; set; }
    public int? TamanhoPagina { get; set; }
    public string? CampoListaResponse { get; set; }
    public string? CampoPaginacaoResponse { get; set; }
    public DateTime? UltimaSincronizacao { get; set; }

    // --- Comportamento ---
    public ComportamentoDuplicidade Duplicidade { get; set; } = ComportamentoDuplicidade.CriarOuAtualizar;
    public string? CampoChaveDuplicidade { get; set; }
    public string? Configuracao { get; set; }

    public ICollection<EtapaFluxoIntegracao> Etapas { get; set; } = [];
}

public enum DirecaoFluxo
{
    Entrada = 0,
    Saida = 1
}

public enum ModoEntrada
{
    Webhook = 0,
    Polling = 1,
    Manual = 2
}

public enum ComportamentoDuplicidade
{
    Criar = 0,
    Atualizar = 1,
    CriarOuAtualizar = 2,
    Ignorar = 3
}

/// <summary>
/// Log de execução de integração
/// </summary>
public class LogIntegracao : EntidadeBase
{
    public long IntegracaoId { get; set; }
    public Integracao Integracao { get; set; } = null!;

    public long? FluxoIntegracaoId { get; set; }
    public FluxoIntegracao? FluxoIntegracao { get; set; }

    public DirecaoFluxo Direcao { get; set; }
    public bool Sucesso { get; set; }
    public string? Endpoint { get; set; }
    public string? MetodoHttp { get; set; }
    public int? StatusCode { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? Erro { get; set; }
    public int DuracaoMs { get; set; }
    public long? RegistroId { get; set; }
    public EntidadeAlvo? EntidadeAlvo { get; set; }
}

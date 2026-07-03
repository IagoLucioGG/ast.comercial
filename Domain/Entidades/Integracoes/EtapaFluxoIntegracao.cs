namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Etapa sequencial dentro de um fluxo de integração de saída.
/// Permite consultas prévias, condições e múltiplas chamadas com contexto compartilhado.
/// </summary>
public class EtapaFluxoIntegracao : EntidadeBase
{
    public long FluxoIntegracaoId { get; set; }
    public FluxoIntegracao FluxoIntegracao { get; set; } = null!;

    public int Ordem { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoEtapaIntegracao Tipo { get; set; }

    public string Endpoint { get; set; } = string.Empty;
    public string MetodoHttp { get; set; } = "POST";

    /// <summary>
    /// Para consultas: parâmetro de busca no endpoint (ex: "cpfCnpj")
    /// </summary>
    public string? CampoChaveConsulta { get; set; }

    /// <summary>
    /// Campo interno de onde vem o valor da chave (ex: "Documento")
    /// </summary>
    public string? CampoInternoChave { get; set; }

    /// <summary>
    /// Variável para armazenar resultado (acessível nas próximas etapas via {{nomeVariavel.campo}})
    /// </summary>
    public string? ArmazenarResultadoComo { get; set; }

    /// <summary>
    /// Campo no response onde está o dado (ex: "data", "resultado", null = raiz)
    /// </summary>
    public string? CampoResultadoResponse { get; set; }

    // Condição para executar
    public string? CondicaoCampo { get; set; }
    public OperadorFiltro? CondicaoOperador { get; set; }
    public string? CondicaoValor { get; set; }

    public bool PararSeErro { get; set; } = true;
    public string? TemplateBody { get; set; }
    public string? Configuracao { get; set; }
}

public enum TipoEtapaIntegracao
{
    Consultar = 0,
    Criar = 1,
    Atualizar = 2,
    CriarOuAtualizar = 3,
    Excluir = 4
}

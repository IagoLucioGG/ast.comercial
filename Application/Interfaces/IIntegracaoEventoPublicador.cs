using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

/// <summary>
/// Publica eventos de saída para integrações externas.
/// Quando uma entidade muda (NegocioGanho, ContatoCriado, etc.), verifica se há FluxoIntegracao
/// com EventoGatilho correspondente e executa o fluxo de saída.
/// </summary>
public interface IIntegracaoEventoPublicador
{
    Task PublicarEventoSaidaAsync(string eventoGatilho, EntidadeAlvo entidade, long registroId, long empresaId, CancellationToken ct = default);
}

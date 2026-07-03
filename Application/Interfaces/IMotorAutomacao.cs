namespace AST.Comercial.Application.Interfaces;

/// <summary>
/// Motor de execução de automações.
/// Processa itens da fila: avalia filtros e executa ações.
/// Preparado para rodar em microserviço separado no futuro.
/// </summary>
public interface IMotorAutomacao
{
    Task ProcessarItemFilaAsync(long filaAutomacaoId, CancellationToken cancellationToken = default);
}

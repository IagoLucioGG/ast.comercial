using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;

namespace AST.Comercial.Application.Interfaces;

public record ItemChecklistNegocioDto(
    long ItemId, string Texto, bool Obrigatorio, int Ordem, bool Concluido,
    long? ConcluidoPorUsuarioId, string? ConcluidoPorNome, DateTime? ConcluidoEm);

public record ChecklistNegocioDto(long ChecklistId, string Nome, int Ordem, List<ItemChecklistNegocioDto> Itens);

public record ChecklistAplicavelDto(
    long NegocioId, long EtapaId, string EtapaNome,
    int Total, int Concluidos, int ObrigatoriosTotal, int ObrigatoriosConcluidos,
    List<ChecklistNegocioDto> Checklists);

public record ProgressoChecklistDto(long NegocioId, int Total, int Concluidos, int ObrigatoriosPendentes);

public interface IChecklistServico
{
    // Configuração (CRUD OData)
    IQueryable<ChecklistEtapa> ObterChecklists();
    Task<ChecklistEtapa> CriarChecklistAsync(Delta<ChecklistEtapa> delta, CancellationToken ct = default);
    Task<ChecklistEtapa?> AtualizarChecklistAsync(long id, Delta<ChecklistEtapa> delta, CancellationToken ct = default);
    Task<bool> RemoverChecklistAsync(long id, CancellationToken ct = default);

    IQueryable<ItemChecklistEtapa> ObterItens();
    Task<ItemChecklistEtapa> CriarItemAsync(Delta<ItemChecklistEtapa> delta, CancellationToken ct = default);
    Task<ItemChecklistEtapa?> AtualizarItemAsync(long id, Delta<ItemChecklistEtapa> delta, CancellationToken ct = default);
    Task<bool> RemoverItemAsync(long id, CancellationToken ct = default);

    // Uso no negócio
    Task<ChecklistAplicavelDto> ObterDoNegocioAsync(long negocioId, CancellationToken ct = default);
    Task<ChecklistAplicavelDto> MarcarItemAsync(long negocioId, long itemId, bool concluido, CancellationToken ct = default);
    Task<List<ProgressoChecklistDto>> ObterProgressoPorFunilAsync(long funilId, CancellationToken ct = default);

    /// <summary>Lança RegraNegocioException (422) se a etapa tiver itens obrigatórios não concluídos para o negócio.</summary>
    Task ValidarObrigatoriosAsync(long negocioId, long etapaId, CancellationToken ct = default);
}

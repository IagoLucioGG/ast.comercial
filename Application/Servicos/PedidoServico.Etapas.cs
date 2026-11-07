using AST.Comercial.Domain.Entidades;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

/// <summary>Etapas do pedido (Orders@Stages da Ploomes). Cada empresa começa com as etapas padrão.</summary>
public partial class PedidoServico
{
    private static readonly (string Nome, string Cor)[] EtapasPadrao =
    [
        ("Novo", "#3b82f6"),
        ("Em andamento", "#f59e0b"),
        ("Concluído", "#10b981"),
        ("Cancelado", "#ef4444"),
    ];

    public IQueryable<EtapaPedido> ObterEtapas() => db.EtapasPedido.AsNoTracking();

    public async Task<EtapaPedido> CriarEtapaAsync(Delta<EtapaPedido> delta, CancellationToken ct = default)
    {
        var etapa = new EtapaPedido();
        delta.Patch(etapa);
        etapa.Id = 0;
        etapa.EmpresaId = db.EmpresaIdAtual;
        etapa.Ativo = true;
        Normalizar(etapa);
        db.EtapasPedido.Add(etapa);
        await db.SaveChangesAsync(ct);
        return etapa;
    }

    public async Task<EtapaPedido?> AtualizarEtapaAsync(long id, Delta<EtapaPedido> delta, CancellationToken ct = default)
    {
        var etapa = await db.EtapasPedido.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (etapa is null) return null;
        var empresaId = etapa.EmpresaId;
        delta.Patch(etapa);
        etapa.Id = id;
        etapa.EmpresaId = empresaId;
        etapa.Ativo = true;
        Normalizar(etapa);
        etapa.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return etapa;
    }

    public async Task<bool> RemoverEtapaAsync(long id, CancellationToken ct = default)
    {
        var etapa = await db.EtapasPedido.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (etapa is null) return false;
        if (await db.Pedidos.AnyAsync(p => p.EtapaPedidoId == id, ct))
            throw new RegraNegocioException("Há pedidos nesta etapa. Mova-os para outra etapa antes de excluir.");
        etapa.Ativo = false;
        etapa.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Primeira etapa da empresa; cria as etapas padrão se ainda não existir nenhuma.</summary>
    private async Task<long> PrimeiraEtapaAsync(CancellationToken ct)
    {
        var primeira = await db.EtapasPedido.OrderBy(e => e.Ordem).ThenBy(e => e.Id).Select(e => (long?)e.Id).FirstOrDefaultAsync(ct);
        if (primeira is not null) return primeira.Value;

        var etapas = EtapasPadrao.Select((e, i) => new EtapaPedido
        {
            EmpresaId = db.EmpresaIdAtual, Nome = e.Nome, Cor = e.Cor, Ordem = i + 1
        }).ToList();
        db.EtapasPedido.AddRange(etapas);
        await db.SaveChangesAsync(ct);
        return etapas[0].Id;
    }

    private static void Normalizar(EtapaPedido e)
    {
        e.Nome = (e.Nome ?? string.Empty).Trim();
        if (e.Nome.Length == 0) throw new RegraNegocioException("Informe o nome da etapa.");
        if (e.Nome.Length > 100) throw new RegraNegocioException("O nome da etapa deve ter no máximo 100 caracteres.");
        e.Cor = string.IsNullOrWhiteSpace(e.Cor) ? null : e.Cor.Trim();
        if (e.Cor is { Length: > 20 }) throw new RegraNegocioException("Cor inválida.");
    }
}

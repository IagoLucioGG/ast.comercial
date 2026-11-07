using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ChecklistServico(AppDbContext db) : IChecklistServico
{
    // ===== Configuração de checklists =====

    public IQueryable<ChecklistEtapa> ObterChecklists() => db.ChecklistsEtapa.AsNoTracking();

    public async Task<ChecklistEtapa> CriarChecklistAsync(Delta<ChecklistEtapa> delta, CancellationToken ct = default)
    {
        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var checklist = new ChecklistEtapa { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(checklist);
        checklist.EmpresaId = db.EmpresaIdAtual;

        if (checklist.EtapaFunilId <= 0 || !await db.EtapasFunil.AnyAsync(e => e.Id == checklist.EtapaFunilId, ct))
            throw new RegraNegocioException("A etapa informada não existe.");
        ValidarChecklist(checklist);

        if (!alterados.Contains(nameof(ChecklistEtapa.Ordem)))
        {
            var max = await db.ChecklistsEtapa.Where(c => c.EtapaFunilId == checklist.EtapaFunilId).MaxAsync(c => (int?)c.Ordem, ct);
            checklist.Ordem = (max ?? -1) + 1;
        }

        db.ChecklistsEtapa.Add(checklist);
        await db.SaveChangesAsync(ct);
        return checklist;
    }

    public async Task<ChecklistEtapa?> AtualizarChecklistAsync(long id, Delta<ChecklistEtapa> delta, CancellationToken ct = default)
    {
        var checklist = await db.ChecklistsEtapa.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (checklist is null) return null;
        var etapaId = checklist.EtapaFunilId;
        var empresaId = checklist.EmpresaId;
        delta.Patch(checklist);
        checklist.EtapaFunilId = etapaId;
        checklist.EmpresaId = empresaId;
        ValidarChecklist(checklist);
        await db.SaveChangesAsync(ct);
        return checklist;
    }

    public async Task<bool> RemoverChecklistAsync(long id, CancellationToken ct = default)
    {
        var checklist = await db.ChecklistsEtapa.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (checklist is null) return false;
        checklist.Ativo = false;
        foreach (var item in checklist.Itens) item.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<ItemChecklistEtapa> ObterItens() => db.ItensChecklistEtapa.AsNoTracking();

    public async Task<ItemChecklistEtapa> CriarItemAsync(Delta<ItemChecklistEtapa> delta, CancellationToken ct = default)
    {
        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var item = new ItemChecklistEtapa { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(item);
        item.EmpresaId = db.EmpresaIdAtual;

        if (item.ChecklistEtapaId <= 0 || !await db.ChecklistsEtapa.AnyAsync(c => c.Id == item.ChecklistEtapaId, ct))
            throw new RegraNegocioException("O checklist informado não existe.");
        ValidarItem(item);

        if (!alterados.Contains(nameof(ItemChecklistEtapa.Ordem)))
        {
            var max = await db.ItensChecklistEtapa.Where(i => i.ChecklistEtapaId == item.ChecklistEtapaId).MaxAsync(i => (int?)i.Ordem, ct);
            item.Ordem = (max ?? -1) + 1;
        }

        db.ItensChecklistEtapa.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<ItemChecklistEtapa?> AtualizarItemAsync(long id, Delta<ItemChecklistEtapa> delta, CancellationToken ct = default)
    {
        var item = await db.ItensChecklistEtapa.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return null;
        var checklistId = item.ChecklistEtapaId;
        var empresaId = item.EmpresaId;
        delta.Patch(item);
        item.ChecklistEtapaId = checklistId;
        item.EmpresaId = empresaId;
        ValidarItem(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> RemoverItemAsync(long id, CancellationToken ct = default)
    {
        var item = await db.ItensChecklistEtapa.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return false;
        item.Ativo = false; // respostas já dadas ficam preservadas
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ===== Checklist de um negócio =====

    public async Task<ChecklistAplicavelDto> ObterDoNegocioAsync(long negocioId, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.AsNoTracking().Where(n => n.Id == negocioId)
            .Select(n => new { n.Id, n.EtapaId, EtapaNome = n.Etapa.Nome }).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Negócio {negocioId} não encontrado.");

        var checklists = await db.ChecklistsEtapa.AsNoTracking()
            .Where(c => c.EtapaFunilId == negocio.EtapaId)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id, c.Nome, c.Ordem,
                Itens = c.Itens.Where(i => i.Ativo).OrderBy(i => i.Ordem).ThenBy(i => i.Id)
                    .Select(i => new { i.Id, i.Texto, i.Obrigatorio, i.Ordem }).ToList()
            })
            .ToListAsync(ct);

        var respostas = await db.RespostasChecklistNegocio.AsNoTracking()
            .Where(r => r.NegocioId == negocioId && r.Concluido)
            .Select(r => new { r.ItemChecklistEtapaId, r.ConcluidoPorUsuarioId, Nome = r.ConcluidoPorUsuario != null ? r.ConcluidoPorUsuario.Nome : null, r.ConcluidoEm })
            .ToListAsync(ct);
        var porItem = respostas.ToDictionary(r => r.ItemChecklistEtapaId);

        var dtos = checklists.Select(c => new ChecklistNegocioDto(c.Id, c.Nome, c.Ordem,
            c.Itens.Select(i =>
            {
                porItem.TryGetValue(i.Id, out var r);
                return new ItemChecklistNegocioDto(i.Id, i.Texto, i.Obrigatorio, i.Ordem, r is not null,
                    r?.ConcluidoPorUsuarioId, r?.Nome, r?.ConcluidoEm);
            }).ToList())).ToList();

        var todos = dtos.SelectMany(c => c.Itens).ToList();
        return new ChecklistAplicavelDto(negocio.Id, negocio.EtapaId, negocio.EtapaNome,
            todos.Count, todos.Count(i => i.Concluido),
            todos.Count(i => i.Obrigatorio), todos.Count(i => i.Obrigatorio && i.Concluido), dtos);
    }

    public async Task<ChecklistAplicavelDto> MarcarItemAsync(long negocioId, long itemId, bool concluido, CancellationToken ct = default)
    {
        var etapaId = await db.Negocios.AsNoTracking().Where(n => n.Id == negocioId).Select(n => (long?)n.EtapaId).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Negócio {negocioId} não encontrado.");

        var item = await db.ItensChecklistEtapa.AsNoTracking()
            .Where(i => i.Id == itemId && i.Ativo && i.ChecklistEtapa.Ativo)
            .Select(i => new { i.Id, i.ChecklistEtapa.EtapaFunilId }).FirstOrDefaultAsync(ct)
            ?? throw new RegraNegocioException("O item do checklist não existe.");
        if (item.EtapaFunilId != etapaId)
            throw new RegraNegocioException("O item não pertence ao checklist da etapa atual do negócio.");

        var resposta = await db.RespostasChecklistNegocio.FirstOrDefaultAsync(r => r.NegocioId == negocioId && r.ItemChecklistEtapaId == itemId, ct);
        if (resposta is null)
        {
            resposta = new RespostaChecklistNegocio { EmpresaId = db.EmpresaIdAtual, NegocioId = negocioId, ItemChecklistEtapaId = itemId };
            db.RespostasChecklistNegocio.Add(resposta);
        }
        resposta.Concluido = concluido;
        resposta.ConcluidoPorUsuarioId = concluido ? db.UsuarioIdAtual : null;
        resposta.ConcluidoEm = concluido ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);

        return await ObterDoNegocioAsync(negocioId, ct);
    }

    public async Task<List<ProgressoChecklistDto>> ObterProgressoPorFunilAsync(long funilId, CancellationToken ct = default)
    {
        // Itens ativos por etapa do funil (uma consulta)
        var itensPorEtapa = await db.ItensChecklistEtapa.AsNoTracking()
            .Where(i => i.Ativo && i.ChecklistEtapa.Ativo && i.ChecklistEtapa.EtapaFunil.FunilId == funilId)
            .GroupBy(i => i.ChecklistEtapa.EtapaFunilId)
            .Select(g => new { EtapaId = g.Key, Total = g.Count(), Obrigatorios = g.Count(x => x.Obrigatorio) })
            .ToListAsync(ct);
        if (itensPorEtapa.Count == 0) return [];
        var etapas = itensPorEtapa.ToDictionary(x => x.EtapaId);
        var etapaIds = etapas.Keys.ToList();

        // Negócios abertos nas etapas que têm checklist (uma consulta)
        var negocios = await db.Negocios.AsNoTracking()
            .Where(n => n.FunilId == funilId && n.FechadoEm == null && etapaIds.Contains(n.EtapaId))
            .Select(n => new { n.Id, n.EtapaId })
            .ToListAsync(ct);
        if (negocios.Count == 0) return [];

        // Itens concluídos por negócio, só da etapa atual dele (uma consulta)
        var concluidos = await db.RespostasChecklistNegocio.AsNoTracking()
            .Where(r => r.Concluido && r.Negocio.FunilId == funilId && r.Negocio.FechadoEm == null
                && r.ItemChecklistEtapa.Ativo && r.ItemChecklistEtapa.ChecklistEtapa.Ativo
                && r.ItemChecklistEtapa.ChecklistEtapa.EtapaFunilId == r.Negocio.EtapaId)
            .GroupBy(r => r.NegocioId)
            .Select(g => new { NegocioId = g.Key, Concluidos = g.Count(), ObrigConcluidos = g.Count(x => x.ItemChecklistEtapa.Obrigatorio) })
            .ToListAsync(ct);
        var porNegocio = concluidos.ToDictionary(c => c.NegocioId);

        return negocios.Select(n =>
        {
            var e = etapas[n.EtapaId];
            porNegocio.TryGetValue(n.Id, out var c);
            return new ProgressoChecklistDto(n.Id, e.Total, c?.Concluidos ?? 0, e.Obrigatorios - (c?.ObrigConcluidos ?? 0));
        }).ToList();
    }

    public async Task ValidarObrigatoriosAsync(long negocioId, long etapaId, CancellationToken ct = default)
    {
        var pendentes = await db.ItensChecklistEtapa.AsNoTracking()
            .Where(i => i.Ativo && i.Obrigatorio && i.ChecklistEtapa.Ativo && i.ChecklistEtapa.EtapaFunilId == etapaId)
            .Where(i => !db.RespostasChecklistNegocio.Any(r => r.NegocioId == negocioId && r.ItemChecklistEtapaId == i.Id && r.Concluido))
            .OrderBy(i => i.ChecklistEtapa.Ordem).ThenBy(i => i.Ordem).ThenBy(i => i.Id)
            .Select(i => i.Texto)
            .ToListAsync(ct);
        if (pendentes.Count == 0) return;

        var nomeEtapa = await db.EtapasFunil.AsNoTracking().Where(e => e.Id == etapaId).Select(e => e.Nome).FirstOrDefaultAsync(ct) ?? $"#{etapaId}";
        throw new RegraNegocioException($"Conclua os itens obrigatórios da etapa {nomeEtapa}: {string.Join("; ", pendentes)}.");
    }

    private static void ValidarChecklist(ChecklistEtapa c)
    {
        c.Nome = (c.Nome ?? string.Empty).Trim();
        if (c.Nome.Length == 0) throw new RegraNegocioException("Informe o nome do checklist.");
        if (c.Nome.Length > 200) throw new RegraNegocioException("O nome do checklist pode ter até 200 caracteres.");
    }

    private static void ValidarItem(ItemChecklistEtapa i)
    {
        i.Texto = (i.Texto ?? string.Empty).Trim();
        if (i.Texto.Length == 0) throw new RegraNegocioException("Informe o texto do item.");
        if (i.Texto.Length > 500) throw new RegraNegocioException("O texto do item pode ter até 500 caracteres.");
    }
}

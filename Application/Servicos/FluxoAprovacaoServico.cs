using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class FluxoAprovacaoServico(AppDbContext db) : IFluxoAprovacaoServico
{
    public IQueryable<FluxoAprovacao> ObterTodos() => db.FluxosAprovacao.AsNoTracking();

    public async Task<FluxoAprovacao?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.FluxosAprovacao.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<FluxoAprovacao> CriarAsync(Delta<FluxoAprovacao> delta, CancellationToken ct = default)
    {
        var entidade = new FluxoAprovacao { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.FluxosAprovacao.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<FluxoAprovacao?> AtualizarAsync(long id, Delta<FluxoAprovacao> delta, CancellationToken ct = default)
    {
        var entidade = await db.FluxosAprovacao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.FluxosAprovacao.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<NivelAprovacao> ObterNiveis() => db.NiveisAprovacao.AsNoTracking();

    public IQueryable<SolicitacaoAprovacao> ObterSolicitacoes() => db.SolicitacoesAprovacao.AsNoTracking();

    public async Task AprovarSolicitacaoAsync(long solicitacaoId, string? observacao, CancellationToken ct = default)
    {
        var solicitacao = await db.SolicitacoesAprovacao
            .Include(s => s.FluxoAprovacao)
                .ThenInclude(f => f.Niveis)
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId, ct)
            ?? throw new KeyNotFoundException($"Solicitação {solicitacaoId} não encontrada.");

        var totalNiveis = solicitacao.FluxoAprovacao.Niveis.Count;

        if (solicitacao.NivelAtual >= totalNiveis)
        {
            solicitacao.Status = StatusSolicitacaoAprovacao.Aprovado;
            solicitacao.AprovadoEm = DateTime.UtcNow;
        }
        else
        {
            solicitacao.NivelAtual++;
            solicitacao.Status = StatusSolicitacaoAprovacao.AprovadoNivel;
        }

        solicitacao.Observacao = observacao;
        await db.SaveChangesAsync(ct);
    }

    public async Task RejeitarSolicitacaoAsync(long solicitacaoId, string? observacao, CancellationToken ct = default)
    {
        var solicitacao = await db.SolicitacoesAprovacao.FirstOrDefaultAsync(s => s.Id == solicitacaoId, ct)
            ?? throw new KeyNotFoundException($"Solicitação {solicitacaoId} não encontrada.");

        solicitacao.Status = StatusSolicitacaoAprovacao.Rejeitado;
        solicitacao.RejeitadoEm = DateTime.UtcNow;
        solicitacao.Observacao = observacao;
        await db.SaveChangesAsync(ct);
    }
}

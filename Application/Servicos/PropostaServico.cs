using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class PropostaServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IPropostaServico
{
    public IQueryable<Proposta> ObterTodos()
        => db.Propostas.AsNoTracking();

    public async Task<Proposta?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Propostas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Proposta> CriarAsync(Delta<Proposta> delta, CancellationToken ct = default)
    {
        var entidade = new Proposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.Propostas.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Proposta?> AtualizarAsync(long id, Delta<Proposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AprovarAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Proposta {id} não encontrada.");
        proposta.StatusAprovacao = StatusAprovacaoProposta.Aprovada;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = proposta.EmpresaId,
            RegistroId = proposta.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = "PropostaAprovada",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public async Task RejeitarAsync(long id, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Proposta {id} não encontrada.");
        proposta.StatusAprovacao = StatusAprovacaoProposta.Rejeitada;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = proposta.EmpresaId,
            RegistroId = proposta.Id,
            EntidadeAlvo = "Proposta",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public IQueryable<ItemProposta> ObterProdutos()
        => db.ItensProposta.AsNoTracking();

    public async Task<ItemProposta> CriarProdutoAsync(Delta<ItemProposta> delta, CancellationToken ct = default)
    {
        var entidade = new ItemProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.ItensProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ItemProposta?> AtualizarProdutoAsync(long id, Delta<ItemProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.ItensProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverProdutoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ItensProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<SecaoProposta> ObterSecoes()
        => db.SecoesProposta.AsNoTracking();

    public async Task<SecaoProposta> CriarSecaoAsync(Delta<SecaoProposta> delta, CancellationToken ct = default)
    {
        var entidade = new SecaoProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.SecoesProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<SecaoProposta?> AtualizarSecaoAsync(long id, Delta<SecaoProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.SecoesProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.SecoesProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<ParcelaProposta> ObterParcelas()
        => db.ParcelasProposta.AsNoTracking();

    public async Task<ParcelaProposta> CriarParcelaAsync(Delta<ParcelaProposta> delta, CancellationToken ct = default)
    {
        var entidade = new ParcelaProposta { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.ParcelasProposta.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ParcelaProposta?> AtualizarParcelaAsync(long id, Delta<ParcelaProposta> delta, CancellationToken ct = default)
    {
        var entidade = await db.ParcelasProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverParcelaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ParcelasProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

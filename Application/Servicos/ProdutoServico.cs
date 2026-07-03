using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ProdutoServico(AppDbContext db) : IProdutoServico
{
    public IQueryable<Produto> ObterTodos()
        => db.Produtos.AsNoTracking();

    public async Task<Produto?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Produto> CriarAsync(Delta<Produto> delta, CancellationToken ct = default)
    {
        var entidade = new Produto { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.Produtos.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<Produto?> AtualizarAsync(long id, Delta<Produto> delta, CancellationToken ct = default)
    {
        var entidade = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<FamiliaProduto> ObterFamilias()
        => db.FamiliasProduto.AsNoTracking();

    public async Task<FamiliaProduto> CriarFamiliaAsync(Delta<FamiliaProduto> delta, CancellationToken ct = default)
    {
        var entidade = new FamiliaProduto { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.FamiliasProduto.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<FamiliaProduto?> AtualizarFamiliaAsync(long id, Delta<FamiliaProduto> delta, CancellationToken ct = default)
    {
        var entidade = await db.FamiliasProduto.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverFamiliaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.FamiliasProduto.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<GrupoProduto> ObterGrupos()
        => db.GruposProduto.AsNoTracking();

    public async Task<GrupoProduto> CriarGrupoAsync(Delta<GrupoProduto> delta, CancellationToken ct = default)
    {
        var entidade = new GrupoProduto { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.GruposProduto.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<GrupoProduto?> AtualizarGrupoAsync(long id, Delta<GrupoProduto> delta, CancellationToken ct = default)
    {
        var entidade = await db.GruposProduto.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverGrupoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.GruposProduto.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

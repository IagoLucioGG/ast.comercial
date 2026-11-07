using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ListaPrecoServico(AppDbContext db) : IListaPrecoServico
{
    // ===== Listas =====
    public IQueryable<ListaPreco> ObterTodas() => db.ListasPreco.AsNoTracking();

    public async Task<ListaPreco?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.ListasPreco.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<ListaPreco> CriarAsync(Delta<ListaPreco> delta, CancellationToken ct = default)
    {
        var entidade = new ListaPreco { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        entidade.EmpresaId = db.EmpresaIdAtual;
        await ValidarListaAsync(entidade, ct);
        db.ListasPreco.Add(entidade);
        if (entidade.Padrao) await DesmarcarOutrasPadraoAsync(null, ct);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ListaPreco?> AtualizarAsync(long id, Delta<ListaPreco> delta, CancellationToken ct = default)
    {
        var entidade = await db.ListasPreco.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entidade is null) return null;
        var empresaId = entidade.EmpresaId;
        delta.Patch(entidade);
        entidade.Id = id;
        entidade.EmpresaId = empresaId;
        await ValidarListaAsync(entidade, ct);
        if (entidade.Padrao) await DesmarcarOutrasPadraoAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ListasPreco.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        entidade.Padrao = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidarListaAsync(ListaPreco l, CancellationToken ct)
    {
        l.Nome = (l.Nome ?? string.Empty).Trim();
        if (l.Nome.Length == 0) throw new RegraNegocioException("Informe o nome da lista de preço.");
        if (l.Nome.Length > 200) throw new RegraNegocioException("O nome da lista deve ter no máximo 200 caracteres.");
        if (l.Descricao is { Length: > 1000 }) throw new RegraNegocioException("A descrição deve ter no máximo 1000 caracteres.");
        if (l.VigenciaInicio.HasValue && l.VigenciaFim.HasValue && l.VigenciaFim.Value < l.VigenciaInicio.Value)
            throw new RegraNegocioException("A vigência é inválida: o fim deve ser igual ou posterior ao início.");
        // O catálogo de moedas é global (EmpresaId = 0) ou da própria empresa.
        if (l.MoedaId.HasValue && !await db.Moedas.IgnoreQueryFilters()
                .AnyAsync(m => m.Id == l.MoedaId.Value && m.Ativo && (m.EmpresaId == 0 || m.EmpresaId == db.EmpresaIdAtual), ct))
            throw new RegraNegocioException("Moeda não encontrada.");
    }

    /// <summary>Garante uma única lista padrão por empresa (o filtro global já restringe ao tenant).</summary>
    private async Task DesmarcarOutrasPadraoAsync(long? idAtual, CancellationToken ct)
    {
        var outras = await db.ListasPreco.Where(l => l.Padrao && (idAtual == null || l.Id != idAtual)).ToListAsync(ct);
        foreach (var o in outras) o.Padrao = false;
    }

    // ===== Itens =====
    public IQueryable<ItemListaPreco> ObterItens() => db.ItensListaPreco.AsNoTracking();

    public async Task<ItemListaPreco?> ObterItemPorIdAsync(long id, CancellationToken ct = default)
        => await db.ItensListaPreco.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<ItemListaPreco> CriarItemAsync(Delta<ItemListaPreco> delta, CancellationToken ct = default)
    {
        var novo = new ItemListaPreco { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(novo);
        if (novo.ListaPrecoId <= 0) throw new RegraNegocioException("Informe a lista de preço.");
        if (novo.ProdutoId <= 0) throw new RegraNegocioException("Informe o produto.");
        ValidarPreco(novo.Preco);
        if (!await db.ListasPreco.AnyAsync(l => l.Id == novo.ListaPrecoId, ct))
            throw new RegraNegocioException("Lista de preço não encontrada.");
        if (!await db.Produtos.AnyAsync(p => p.Id == novo.ProdutoId, ct))
            throw new RegraNegocioException("Produto não encontrado.");

        // O índice único (lista, produto) vale também para itens excluídos: reativa em vez de duplicar.
        var existente = await db.ItensListaPreco.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.EmpresaId == db.EmpresaIdAtual && i.ListaPrecoId == novo.ListaPrecoId && i.ProdutoId == novo.ProdutoId, ct);
        if (existente is not null)
        {
            if (existente.Ativo) throw new RegraNegocioException("Este produto já está na lista de preço.");
            existente.Ativo = true;
            existente.Preco = novo.Preco;
            await db.SaveChangesAsync(ct);
            return existente;
        }
        novo.EmpresaId = db.EmpresaIdAtual;
        db.ItensListaPreco.Add(novo);
        await db.SaveChangesAsync(ct);
        return novo;
    }

    public async Task<ItemListaPreco?> AtualizarItemAsync(long id, Delta<ItemListaPreco> delta, CancellationToken ct = default)
    {
        var entidade = await db.ItensListaPreco.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entidade is null) return null;
        var (listaId, produtoId, empresaId) = (entidade.ListaPrecoId, entidade.ProdutoId, entidade.EmpresaId);
        delta.Patch(entidade);
        // Só o preço é editável: lista e produto identificam o item.
        entidade.Id = id;
        entidade.ListaPrecoId = listaId;
        entidade.ProdutoId = produtoId;
        entidade.EmpresaId = empresaId;
        ValidarPreco(entidade.Preco);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverItemAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ItensListaPreco.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void ValidarPreco(decimal preco)
    {
        if (preco < 0) throw new RegraNegocioException("O preço não pode ser negativo.");
        if (preco > 9_999_999_999_999m) throw new RegraNegocioException("Preço acima do limite permitido.");
    }

    // ===== Operações compostas =====
    public async Task<ResultadoCopiaPrecos> CopiarPrecosBaseAsync(long listaId, bool sobrescrever, CancellationToken ct = default)
    {
        var lista = await db.ListasPreco.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listaId, ct)
            ?? throw new RegraNegocioException("Lista de preço não encontrada.");

        var produtos = await db.Produtos.AsNoTracking().Select(p => new { p.Id, p.Preco, p.MoedaId }).ToListAsync(ct);
        var itens = await db.ItensListaPreco.IgnoreQueryFilters()
            .Where(i => i.EmpresaId == db.EmpresaIdAtual && i.ListaPrecoId == listaId).ToListAsync(ct);
        var porProduto = itens.ToDictionary(i => i.ProdutoId);

        int criados = 0, atualizados = 0, ignorados = 0;
        foreach (var p in produtos)
        {
            // Preço em outra moeda não é copiado para uma lista com moeda definida.
            if (lista.MoedaId.HasValue && p.MoedaId.HasValue && p.MoedaId != lista.MoedaId) { ignorados++; continue; }
            if (porProduto.TryGetValue(p.Id, out var item))
            {
                if (item.Ativo && !sobrescrever) { ignorados++; continue; }
                item.Ativo = true;
                item.Preco = p.Preco;
                atualizados++;
            }
            else
            {
                db.ItensListaPreco.Add(new ItemListaPreco { EmpresaId = db.EmpresaIdAtual, ListaPrecoId = listaId, ProdutoId = p.Id, Preco = p.Preco });
                criados++;
            }
        }
        await db.SaveChangesAsync(ct);
        return new ResultadoCopiaPrecos(criados, atualizados, ignorados);
    }

    public async Task<PrecosProdutoDto?> ObterPrecosProdutoAsync(long produtoId, bool todas, CancellationToken ct = default)
    {
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId, ct);
        if (produto is null) return null;

        var moedas = await db.Moedas.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.EmpresaId == 0 || m.EmpresaId == db.EmpresaIdAtual).ToDictionaryAsync(m => m.Id, ct);
        string? Cod(long? id) => id.HasValue && moedas.TryGetValue(id.Value, out var m) ? m.Codigo : null;
        string? Sim(long? id) => id.HasValue && moedas.TryGetValue(id.Value, out var m) ? m.Simbolo : null;

        var hoje = DateTime.UtcNow.Date;
        var listas = await db.ListasPreco.AsNoTracking().OrderByDescending(l => l.Padrao).ThenBy(l => l.Nome).ToListAsync(ct);
        var itens = await db.ItensListaPreco.AsNoTracking().Where(i => i.ProdutoId == produtoId).ToDictionaryAsync(i => i.ListaPrecoId, ct);

        var resultado = new List<PrecoListaDto>();
        foreach (var l in listas)
        {
            var vigente = (!l.VigenciaInicio.HasValue || l.VigenciaInicio.Value.Date <= hoje)
                          && (!l.VigenciaFim.HasValue || l.VigenciaFim.Value.Date >= hoje);
            itens.TryGetValue(l.Id, out var item);
            if (!todas && (!vigente || item is null)) continue;
            resultado.Add(new PrecoListaDto(l.Id, l.Nome, l.Padrao, vigente, l.VigenciaInicio, l.VigenciaFim,
                l.MoedaId, Cod(l.MoedaId), Sim(l.MoedaId), item?.Id, item?.Preco));
        }
        return new PrecosProdutoDto(produto.Id, produto.Nome, produto.Preco,
            produto.MoedaId, Cod(produto.MoedaId), Sim(produto.MoedaId), resultado);
    }
}

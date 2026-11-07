using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ItemNegocioServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IItemNegocioServico
{
    public IQueryable<ItemNegocio> ObterTodos() => db.ItensNegocio.AsNoTracking();

    /// <summary>Total = quantidade × preço × (1 − desconto/100), com 2 casas.</summary>
    public static decimal CalcularTotal(decimal quantidade, decimal preco, decimal descontoPercentual)
        => Math.Round(quantidade * preco * (1 - descontoPercentual / 100m), 2, MidpointRounding.AwayFromZero);

    public async Task<ItemNegocio> CriarAsync(Delta<ItemNegocio> delta, CancellationToken ct = default)
    {
        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var item = new ItemNegocio { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(item);
        item.EmpresaId = db.EmpresaIdAtual;

        if (item.NegocioId <= 0 || !await db.Negocios.AnyAsync(n => n.Id == item.NegocioId, ct))
            throw new RegraNegocioException("O negócio informado não existe.");

        if (item.ProdutoId is not null)
        {
            var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProdutoId, ct)
                ?? throw new RegraNegocioException("O produto informado não existe.");
            if (!alterados.Contains(nameof(ItemNegocio.Descricao)) || string.IsNullOrWhiteSpace(item.Descricao))
                item.Descricao = produto.Nome;
            if (!alterados.Contains(nameof(ItemNegocio.PrecoUnitario)))
                item.PrecoUnitario = (await ObterPrecoSugeridoAsync(produto.Id, item.NegocioId, ct)).Preco;
        }

        if (!alterados.Contains(nameof(ItemNegocio.Ordem)))
        {
            var maxOrdem = await db.ItensNegocio.Where(i => i.NegocioId == item.NegocioId).MaxAsync(i => (int?)i.Ordem, ct);
            item.Ordem = (maxOrdem ?? -1) + 1;
        }

        Validar(item);
        item.Total = CalcularTotal(item.Quantidade, item.PrecoUnitario, item.Desconto);

        db.ItensNegocio.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<ItemNegocio?> AtualizarAsync(long id, Delta<ItemNegocio> delta, CancellationToken ct = default)
    {
        var item = await db.ItensNegocio.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return null;

        var alterados = delta.GetChangedPropertyNames().ToHashSet();
        var negocioId = item.NegocioId;
        var empresaId = item.EmpresaId;
        var produtoAnterior = item.ProdutoId;

        delta.Patch(item);
        item.NegocioId = negocioId; // o item nunca troca de negócio
        item.EmpresaId = empresaId;

        if (item.ProdutoId is not null && item.ProdutoId != produtoAnterior)
        {
            var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProdutoId, ct)
                ?? throw new RegraNegocioException("O produto informado não existe.");
            if (!alterados.Contains(nameof(ItemNegocio.Descricao))) item.Descricao = produto.Nome;
            if (!alterados.Contains(nameof(ItemNegocio.PrecoUnitario)))
                item.PrecoUnitario = (await ObterPrecoSugeridoAsync(produto.Id, negocioId, ct)).Preco;
        }

        Validar(item);
        item.Total = CalcularTotal(item.Quantidade, item.PrecoUnitario, item.Desconto);

        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var item = await db.ItensNegocio.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return false;
        item.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ReordenarAsync(long negocioId, IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        if (!await db.Negocios.AnyAsync(n => n.Id == negocioId, ct))
            throw new KeyNotFoundException($"Negócio {negocioId} não encontrado.");

        var itens = await db.ItensNegocio.Where(i => i.NegocioId == negocioId).ToListAsync(ct);
        if (ids.Distinct().Count() != ids.Count)
            throw new RegraNegocioException("A lista de itens para ordenar tem repetidos.");
        var porId = itens.ToDictionary(i => i.Id);
        if (ids.Any(i => !porId.ContainsKey(i)))
            throw new RegraNegocioException("Há itens que não pertencem a este negócio.");

        var ordem = 0;
        foreach (var itemId in ids) porId[itemId].Ordem = ordem++;
        // Itens não citados ficam depois, na ordem em que já estavam
        foreach (var restante in itens.Where(i => !ids.Contains(i.Id)).OrderBy(i => i.Ordem).ThenBy(i => i.Id))
            restante.Ordem = ordem++;

        await db.SaveChangesAsync(ct);
    }

    public async Task<ValorPelosItensDto> AtualizarValorPelosItensAsync(long negocioId, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId, ct)
            ?? throw new KeyNotFoundException($"Negócio {negocioId} não encontrado.");

        var totais = await db.ItensNegocio.AsNoTracking().Where(i => i.NegocioId == negocioId).Select(i => i.Total).ToListAsync(ct);
        if (totais.Count == 0)
            throw new RegraNegocioException("O negócio não tem produtos para somar.");

        negocio.Valor = Math.Round(totais.Sum(), 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new ValorPelosItensDto(negocio.Id, negocio.Valor.Value, negocio.MoedaId, totais.Count);
    }

    public async Task<PrecoSugeridoDto> ObterPrecoSugeridoAsync(long produtoId, long? negocioId, CancellationToken ct = default)
    {
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId, ct)
            ?? throw new RegraNegocioException("O produto informado não existe.");

        long? moedaNegocio = null;
        if (negocioId is not null)
            moedaNegocio = await db.Negocios.AsNoTracking().Where(n => n.Id == negocioId).Select(n => n.MoedaId).FirstOrDefaultAsync(ct);

        var agora = DateTime.UtcNow;
        var candidatos = await db.ItensListaPreco.AsNoTracking()
            .Where(i => i.ProdutoId == produtoId && i.ListaPreco.Padrao && i.ListaPreco.Ativo
                && (i.ListaPreco.VigenciaInicio == null || i.ListaPreco.VigenciaInicio <= agora)
                && (i.ListaPreco.VigenciaFim == null || i.ListaPreco.VigenciaFim >= agora))
            .Select(i => new { i.Preco, i.ListaPrecoId, i.ListaPreco.Nome, i.ListaPreco.MoedaId })
            .ToListAsync(ct);

        // Lista em outra moeda que a do negócio não serve (o preço estaria na moeda errada)
        var escolhido = candidatos
            .Where(c => c.MoedaId is null || moedaNegocio is null || c.MoedaId == moedaNegocio)
            .OrderByDescending(c => moedaNegocio is not null && c.MoedaId == moedaNegocio)
            .ThenBy(c => c.ListaPrecoId)
            .FirstOrDefault();

        return escolhido is null
            ? new PrecoSugeridoDto(produto.Preco, "Produto", null, null)
            : new PrecoSugeridoDto(escolhido.Preco, "ListaPreco", escolhido.ListaPrecoId, escolhido.Nome);
    }

    private static void Validar(ItemNegocio item)
    {
        item.Descricao = (item.Descricao ?? string.Empty).Trim();
        if (item.Descricao.Length == 0) throw new RegraNegocioException("Informe a descrição do item.");
        if (item.Descricao.Length > 300) throw new RegraNegocioException("A descrição do item pode ter até 300 caracteres.");
        if (item.Quantidade <= 0) throw new RegraNegocioException("A quantidade deve ser maior que zero.");
        if (item.Quantidade > 99_999_999m) throw new RegraNegocioException("Quantidade acima do limite permitido.");
        if (item.PrecoUnitario < 0) throw new RegraNegocioException("O preço não pode ser negativo.");
        if (item.PrecoUnitario > 9_999_999_999m) throw new RegraNegocioException("Preço acima do limite permitido.");
        if (item.Desconto < 0 || item.Desconto > 100) throw new RegraNegocioException("O desconto deve ficar entre 0 e 100%.");
    }
}

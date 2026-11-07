using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

/// <summary>CRUD de pedidos e geração a partir de proposta. Etapas ficam em PedidoServico.Etapas.cs.</summary>
public partial class PedidoServico(AppDbContext db) : IPedidoServico
{
    public IQueryable<Pedido> ObterTodos() => db.Pedidos.AsNoTracking();

    public async Task<Pedido?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Pedido> CriarAsync(Delta<Pedido> delta, CancellationToken ct = default)
    {
        var pedido = new Pedido();
        delta.Patch(pedido);
        pedido.Id = 0;
        pedido.EmpresaId = db.EmpresaIdAtual;
        pedido.Ativo = true;
        pedido.PropostaId = null; // a origem só é definida por GerarDePropostaAsync
        pedido.Itens = [];
        pedido.Data = pedido.Data == default ? DateTime.UtcNow : ParaUtc(pedido.Data);
        await ValidarAsync(pedido, ct);
        pedido.EtapaPedidoId ??= await PrimeiraEtapaAsync(ct);
        pedido.Numero = await ProximoNumeroAsync(ct);
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        return pedido;
    }

    public async Task<Pedido?> AtualizarAsync(long id, Delta<Pedido> delta, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return null;
        var (empresaId, numero, propostaId) = (pedido.EmpresaId, pedido.Numero, pedido.PropostaId);
        delta.Patch(pedido);
        pedido.Id = id;
        pedido.EmpresaId = empresaId;
        pedido.Numero = numero;
        pedido.PropostaId = propostaId;
        pedido.Ativo = true;
        pedido.Data = ParaUtc(pedido.Data);
        await ValidarAsync(pedido, ct);
        pedido.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return pedido;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return false;
        pedido.Ativo = false;
        pedido.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Pedido?> GerarDePropostaAsync(long propostaId, CancellationToken ct = default)
    {
        var proposta = await db.Propostas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == propostaId, ct);
        if (proposta is null) return null;

        if (proposta.IsTemplate)
            throw new RegraNegocioException("Um modelo de proposta não gera pedido.");
        var liberada = proposta.StatusAprovacao == StatusAprovacaoProposta.Aprovada
                       || proposta.StatusResposta == StatusRespostaProposta.Aceita;
        if (!liberada)
            throw new RegraNegocioException("Só uma proposta aprovada ou aceita pelo cliente pode gerar pedido.");
        if (await db.Pedidos.AnyAsync(p => p.PropostaId == propostaId, ct))
            throw new RegraNegocioException("Esta proposta já gerou um pedido.");

        var itens = await db.ItensProposta.AsNoTracking()
            .Where(i => i.PropostaId == propostaId).OrderBy(i => i.Ordem).ToListAsync(ct);

        var pedido = new Pedido
        {
            EmpresaId = db.EmpresaIdAtual,
            Titulo = proposta.Titulo,
            Observacoes = proposta.Observacoes,
            Data = DateTime.UtcNow,
            Valor = proposta.Valor,
            Desconto = proposta.Desconto,
            CustoFrete = proposta.CustoFrete,
            NumeroParcelas = proposta.NumeroParcelas,
            FormaPagamento = proposta.FormaPagamento,
            MoedaId = proposta.MoedaId,
            ClienteId = proposta.ClienteId,
            NegocioId = proposta.NegocioId,
            PropostaId = proposta.Id,
            ResponsavelId = proposta.ResponsavelId ?? db.UsuarioIdAtual,
            EtapaPedidoId = await PrimeiraEtapaAsync(ct),
            Numero = await ProximoNumeroAsync(ct),
            Itens = [.. itens.Select(i => new ItemPedido
            {
                EmpresaId = db.EmpresaIdAtual,
                ProdutoId = i.ProdutoId,
                ProdutoNome = i.ProdutoNome,
                ProdutoCodigo = i.ProdutoCodigo,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                Desconto = i.Desconto,
                Total = i.Total,
                Ordem = i.Ordem,
                MoedaId = i.MoedaId
            })]
        };

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        return await ObterPorIdAsync(pedido.Id, ct);
    }

    // ------------------------------------------------------------------ apoio

    private async Task<int> ProximoNumeroAsync(CancellationToken ct)
        => (await db.Pedidos.IgnoreQueryFilters()
               .Where(p => p.EmpresaId == db.EmpresaIdAtual)
               .MaxAsync(p => (int?)p.Numero, ct) ?? 0) + 1;

    private async Task ValidarAsync(Pedido p, CancellationToken ct)
    {
        if (p.Titulo is { Length: > 300 }) throw new RegraNegocioException("O título deve ter no máximo 300 caracteres.");
        if (p.Valor < 0 || p.Desconto < 0 || p.CustoFrete < 0) throw new RegraNegocioException("Valores não podem ser negativos.");
        if (p.ClienteId is not null && !await db.Clientes.AnyAsync(x => x.Id == p.ClienteId, ct))
            throw new RegraNegocioException("Cliente não encontrado.");
        if (p.NegocioId is not null && !await db.Negocios.AnyAsync(x => x.Id == p.NegocioId, ct))
            throw new RegraNegocioException("Negócio não encontrado.");
        if (p.EtapaPedidoId is not null && !await db.EtapasPedido.AnyAsync(x => x.Id == p.EtapaPedidoId, ct))
            throw new RegraNegocioException("Etapa não encontrada.");
        if (p.MoedaId is not null && !await db.Moedas.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == p.MoedaId && x.Ativo && (x.EmpresaId == 0 || x.EmpresaId == db.EmpresaIdAtual), ct))
            throw new RegraNegocioException("Moeda não encontrada.");
    }

    private static DateTime ParaUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc),
    };
}

using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AST.Comercial.Infrastructure.Integracoes;

/// <summary>
/// Implementação que verifica fluxos de saída com EventoGatilho correspondente
/// e executa o ExecutorFluxoSaida para cada um.
/// </summary>
public class IntegracaoEventoPublicador(AppDbContext db, ExecutorFluxoSaida executor, ILogger<IntegracaoEventoPublicador> logger) : IIntegracaoEventoPublicador
{
    public async Task PublicarEventoSaidaAsync(string eventoGatilho, EntidadeAlvo entidade, long registroId, long empresaId, CancellationToken ct = default)
    {
        var fluxos = await db.FluxosIntegracao
            .Include(f => f.Integracao)
            .Where(f => f.Ativo
                && f.Direcao == DirecaoFluxo.Saida
                && f.EventoGatilho == eventoGatilho
                && f.EntidadeAlvo == entidade
                && f.EmpresaId == empresaId
                && f.Integracao.Ativo
                && f.Integracao.Status == StatusIntegracao.Ligado)
            .ToListAsync(ct);

        if (fluxos.Count == 0) return;

        var dados = await CarregarDadosRegistroAsync(entidade, registroId, ct);
        if (dados is null) return;

        foreach (var fluxo in fluxos)
        {
            try
            {
                await executor.ExecutarAsync(fluxo, dados, null, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao executar fluxo de saída {FluxoId} para evento {Evento}", fluxo.Id, eventoGatilho);
                db.LogsIntegracao.Add(new LogIntegracao
                {
                    EmpresaId = empresaId,
                    IntegracaoId = fluxo.IntegracaoId,
                    FluxoIntegracaoId = fluxo.Id,
                    Direcao = DirecaoFluxo.Saida,
                    Sucesso = false,
                    Erro = ex.Message,
                    RegistroId = registroId,
                    EntidadeAlvo = entidade
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task<Dictionary<string, object?>?> CarregarDadosRegistroAsync(EntidadeAlvo entidade, long registroId, CancellationToken ct)
    {
        return entidade switch
        {
            EntidadeAlvo.Cliente => await CarregarClienteAsync(registroId, ct),
            EntidadeAlvo.Negocio => await CarregarNegocioAsync(registroId, ct),
            EntidadeAlvo.Produto => await CarregarProdutoAsync(registroId, ct),
            _ => null
        };
    }

    private async Task<Dictionary<string, object?>?> CarregarClienteAsync(long id, CancellationToken ct)
    {
        var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null) return null;

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = cliente.Id,
            ["Nome"] = cliente.Nome,
            ["Documento"] = cliente.Documento,
            ["Email"] = cliente.Email,
            ["Telefone"] = cliente.Telefone,
            ["ChaveExterna"] = cliente.ChaveExterna
        };
    }

    private async Task<Dictionary<string, object?>?> CarregarNegocioAsync(long id, CancellationToken ct)
    {
        var negocio = await db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
        if (negocio is null) return null;

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = negocio.Id,
            ["Titulo"] = negocio.Titulo,
            ["Valor"] = negocio.Valor,
            ["ClienteId"] = negocio.ClienteId,
            ["FunilId"] = negocio.FunilId,
            ["EtapaId"] = negocio.EtapaId,
            ["StatusId"] = negocio.StatusId,
            ["FechadoEm"] = negocio.FechadoEm,
            ["PrevisaoFechamento"] = negocio.PrevisaoFechamento,
            ["ChaveExterna"] = negocio.ChaveExterna
        };
    }

    private async Task<Dictionary<string, object?>?> CarregarProdutoAsync(long id, CancellationToken ct)
    {
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return null;

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = produto.Id,
            ["Nome"] = produto.Nome,
            ["Codigo"] = produto.Codigo,
            ["Preco"] = produto.Preco,
            ["ChaveExterna"] = produto.ChaveExterna
        };
    }
}

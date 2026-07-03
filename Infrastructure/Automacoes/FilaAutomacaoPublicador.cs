using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Automacoes;

/// <summary>
/// Publica eventos na fila de automações.
/// 1. Busca automações ativas que batem com o gatilho
/// 2. Insere na tabela FilaAutomacao
/// 3. Envia pg_notify para o processador reagir imediatamente
/// </summary>
public class FilaAutomacaoPublicador(AppDbContext db) : IFilaAutomacaoPublicador
{
    public async Task PublicarEventoAsync(EventoAutomacao evento, CancellationToken cancellationToken = default)
    {
        var gatilho = Enum.Parse<TipoGatilho>(evento.Gatilho);
        var entidadeAlvo = Enum.Parse<EntidadeAlvo>(evento.EntidadeAlvo);

        var automacoes = await db.Automacoes
            .IgnoreQueryFilters()
            .Where(a => a.EmpresaId == evento.EmpresaId
                && a.EntidadeAlvo == entidadeAlvo
                && a.Gatilho == gatilho
                && a.Ativo)
            .Select(a => new { a.Id, a.ImpedirCascata })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (automacoes.Count == 0) return;

        var itens = new List<FilaAutomacao>();
        foreach (var automacao in automacoes)
        {
            if (automacao.ImpedirCascata && evento.AutomacaoOrigemId is not null)
                continue;

            itens.Add(new FilaAutomacao
            {
                EmpresaId = evento.EmpresaId,
                AutomacaoId = automacao.Id,
                RegistroId = evento.RegistroId,
                EntidadeAlvo = entidadeAlvo,
                Gatilho = gatilho,
                DadosEvento = evento.DadosEvento,
                DisparadoPor = evento.DisparadoPor,
                AutomacaoOrigemId = evento.AutomacaoOrigemId
            });
        }

        if (itens.Count == 0) return;

        db.FilaAutomacao.AddRange(itens);
        await db.SaveChangesAsync(cancellationToken);

        // pg_notify para processamento imediato
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_notify('fila_automacao', @p0)",
            [itens.Count.ToString()],
            cancellationToken);
    }
}

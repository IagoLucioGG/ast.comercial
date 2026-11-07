using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class FunilServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IFunilServico
{
    // Retorna a entidade (e não DTO) para casar com o EDM "Funis" e permitir $expand/$select/$count.
    public IQueryable<Funil> ObterTodos()
        => db.Funis.AsNoTracking();

    public async Task<Funil?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Funis.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Funil> CriarAsync(Delta<Funil> delta, CancellationToken ct = default)
    {
        var funil = new Funil { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(funil);
        funil.EmpresaId = db.EmpresaIdAtual;

        db.Funis.Add(funil);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return funil;
    }

    public async Task<Funil?> AtualizarAsync(long id, Delta<Funil> delta, CancellationToken ct = default)
    {
        var funil = await db.Funis.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (funil is null) return null;

        delta.Patch(funil);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return funil;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var funil = await db.Funis.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (funil is null) return false;

        funil.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = funil.EmpresaId,
            RegistroId = funil.Id,
            EntidadeAlvo = "Funil",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public IQueryable<EtapaFunil> ObterEtapas(long funilId)
        => db.EtapasFunil.AsNoTracking().Where(e => e.FunilId == funilId);

    public async Task<EtapaFunil> CriarEtapaAsync(long funilId, Delta<EtapaFunil> delta, CancellationToken ct = default)
    {
        var funilExiste = await db.Funis.AnyAsync(f => f.Id == funilId, ct);
        if (!funilExiste) throw new KeyNotFoundException($"Funil {funilId} não encontrado.");

        var etapa = new EtapaFunil();
        delta.Patch(etapa);
        etapa.FunilId = funilId;
        etapa.EmpresaId = db.EmpresaIdAtual;

        db.EtapasFunil.Add(etapa);
        await db.SaveChangesAsync(ct);

        return etapa;
    }
}

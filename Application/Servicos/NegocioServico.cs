using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class NegocioServico(AppDbContext db, IFilaAutomacaoPublicador publicador, IIntegracaoEventoPublicador integracaoPublicador) : INegocioServico
{
    public IQueryable<Negocio> ObterTodos()
        => db.Negocios.AsNoTracking();

    public async Task<Negocio?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<NegocioDto> CriarAsync(Delta<Negocio> delta, CancellationToken ct = default)
    {
        var negocio = new Negocio();
        delta.Patch(negocio);

        db.Negocios.Add(negocio);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new NegocioDto
        {
            Id = negocio.Id,
            Titulo = negocio.Titulo,
            Valor = negocio.Valor,
            StatusId = negocio.StatusId,
            PrevisaoFechamento = negocio.PrevisaoFechamento,
            ClienteId = negocio.ClienteId,
            FunilId = negocio.FunilId,
            EtapaId = negocio.EtapaId,
            CriadoEm = negocio.CriadoEm,
        };
    }

    public async Task<NegocioDto?> AtualizarAsync(long id, Delta<Negocio> delta, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (negocio is null) return null;

        delta.Patch(negocio);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new NegocioDto
        {
            Id = negocio.Id,
            Titulo = negocio.Titulo,
            Valor = negocio.Valor,
            StatusId = negocio.StatusId,
            FechadoEm = negocio.FechadoEm,
            PrevisaoFechamento = negocio.PrevisaoFechamento,
            ClienteId = negocio.ClienteId,
            FunilId = negocio.FunilId,
            EtapaId = negocio.EtapaId,
            CriadoEm = negocio.CriadoEm,
        };
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (negocio is null) return false;

        negocio.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public async Task GanharAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        negocio.FechadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        await integracaoPublicador.PublicarEventoSaidaAsync("NegocioGanho", Domain.Entidades.EntidadeAlvo.Negocio, negocio.Id, negocio.EmpresaId, ct);
    }

    public async Task PerderAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        negocio.FechadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public async Task ReabrirAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        negocio.FechadoEm = null;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public IQueryable<StatusNegocio> ObterStatus()
        => db.StatusNegocios.AsNoTracking();

    public async Task<StatusNegocio> CriarStatusAsync(Delta<StatusNegocio> delta, CancellationToken ct = default)
    {
        var entidade = new StatusNegocio { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.StatusNegocios.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<StatusNegocio?> AtualizarStatusAsync(long id, Delta<StatusNegocio> delta, CancellationToken ct = default)
    {
        var entidade = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverStatusAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<MotivoPerda> ObterMotivosPerda()
        => db.MotivosPerda.AsNoTracking();

    public async Task<MotivoPerda> CriarMotivoPerdaAsync(Delta<MotivoPerda> delta, CancellationToken ct = default)
    {
        var entidade = new MotivoPerda { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.MotivosPerda.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<MotivoPerda?> AtualizarMotivoPerdaAsync(long id, Delta<MotivoPerda> delta, CancellationToken ct = default)
    {
        var entidade = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverMotivoPerdaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<Funil> ObterFunis()
        => db.Funis.AsNoTracking();

    public IQueryable<EtapaFunil> ObterEtapas()
        => db.EtapasFunil.AsNoTracking();
}


using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class FunilServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IFunilServico
{
    public IQueryable<FunilDto> ObterTodos()
    {
        return db.Funis.AsNoTracking().Select(f => new FunilDto
        {
            Id = f.Id,
            Nome = f.Nome,
            Descricao = f.Descricao,
            Ordem = f.Ordem,
            CriadoEm = f.CriadoEm,
        });
    }

    public async Task<FunilDto?> ObterPorIdAsync(long id, CancellationToken ct = default)
    {
        return await db.Funis.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FunilDto
            {
                Id = f.Id,
                Nome = f.Nome,
                Descricao = f.Descricao,
                Ordem = f.Ordem,
                CriadoEm = f.CriadoEm,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FunilDto> CriarAsync(Delta<Funil> delta, CancellationToken ct = default)
    {
        var funil = new Funil { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(funil);

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

        return new FunilDto
        {
            Id = funil.Id,
            Nome = funil.Nome,
            Descricao = funil.Descricao,
            Ordem = funil.Ordem,
            CriadoEm = funil.CriadoEm,
        };
    }

    public async Task<FunilDto?> AtualizarAsync(long id, Delta<Funil> delta, CancellationToken ct = default)
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

        return new FunilDto
        {
            Id = funil.Id,
            Nome = funil.Nome,
            Descricao = funil.Descricao,
            Ordem = funil.Ordem,
            CriadoEm = funil.CriadoEm,
        };
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

    public IQueryable<EtapaFunilDto> ObterEtapas(long funilId)
    {
        return db.EtapasFunil.AsNoTracking()
            .Where(e => e.FunilId == funilId)
            .Select(e => new EtapaFunilDto
            {
                Id = e.Id,
                Nome = e.Nome,
                Ordem = e.Ordem,
                DiasParaExpirar = e.DiasParaExpirar,
                FunilId = e.FunilId,
                CriadoEm = e.CriadoEm,
            });
    }

    public async Task<EtapaFunilDto> CriarEtapaAsync(Delta<EtapaFunil> delta, CancellationToken ct = default)
    {
        var etapa = new EtapaFunil { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(etapa);

        db.EtapasFunil.Add(etapa);
        await db.SaveChangesAsync(ct);

        return new EtapaFunilDto
        {
            Id = etapa.Id,
            Nome = etapa.Nome,
            Ordem = etapa.Ordem,
            DiasParaExpirar = etapa.DiasParaExpirar,
            FunilId = etapa.FunilId,
            CriadoEm = etapa.CriadoEm,
        };
    }
}

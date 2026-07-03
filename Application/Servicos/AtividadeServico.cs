using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class AtividadeServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IAtividadeServico
{
    public IQueryable<AtividadeDto> ObterTodos()
    {
        return db.Atividades.AsNoTracking().Select(a => new AtividadeDto
        {
            Id = a.Id,
            Titulo = a.Titulo,
            Descricao = a.Descricao,
            Tipo = a.Tipo,
            DataVencimento = a.DataVencimento,
            ConcluidaEm = a.ConcluidaEm,
            Concluida = a.Concluida,
            ClienteId = a.ClienteId,
            PessoaContatoId = a.PessoaContatoId,
            NegocioId = a.NegocioId,
            CriadoEm = a.CriadoEm,
        });
    }

    public async Task<AtividadeDto?> ObterPorIdAsync(long id, CancellationToken ct = default)
    {
        return await db.Atividades.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AtividadeDto
            {
                Id = a.Id,
                Titulo = a.Titulo,
                Descricao = a.Descricao,
                Tipo = a.Tipo,
                DataVencimento = a.DataVencimento,
                ConcluidaEm = a.ConcluidaEm,
                Concluida = a.Concluida,
                ClienteId = a.ClienteId,
                PessoaContatoId = a.PessoaContatoId,
                NegocioId = a.NegocioId,
                CriadoEm = a.CriadoEm,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AtividadeDto> CriarAsync(Delta<Atividade> delta, CancellationToken ct = default)
    {
        var atividade = new Atividade { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(atividade);

        db.Atividades.Add(atividade);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = atividade.EmpresaId,
            RegistroId = atividade.Id,
            EntidadeAlvo = "Atividade",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new AtividadeDto
        {
            Id = atividade.Id,
            Titulo = atividade.Titulo,
            Descricao = atividade.Descricao,
            Tipo = atividade.Tipo,
            DataVencimento = atividade.DataVencimento,
            Concluida = atividade.Concluida,
            ClienteId = atividade.ClienteId,
            PessoaContatoId = atividade.PessoaContatoId,
            NegocioId = atividade.NegocioId,
            CriadoEm = atividade.CriadoEm,
        };
    }

    public async Task<AtividadeDto?> AtualizarAsync(long id, Delta<Atividade> delta, CancellationToken ct = default)
    {
        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (atividade is null) return null;

        delta.Patch(atividade);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = atividade.EmpresaId,
            RegistroId = atividade.Id,
            EntidadeAlvo = "Atividade",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new AtividadeDto
        {
            Id = atividade.Id,
            Titulo = atividade.Titulo,
            Descricao = atividade.Descricao,
            Tipo = atividade.Tipo,
            DataVencimento = atividade.DataVencimento,
            ConcluidaEm = atividade.ConcluidaEm,
            Concluida = atividade.Concluida,
            ClienteId = atividade.ClienteId,
            PessoaContatoId = atividade.PessoaContatoId,
            NegocioId = atividade.NegocioId,
            CriadoEm = atividade.CriadoEm,
        };
    }

    public async Task<bool> ConcluirAsync(long id, CancellationToken ct = default)
    {
        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (atividade is null) return false;

        atividade.Concluida = true;
        atividade.ConcluidaEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = atividade.EmpresaId,
            RegistroId = atividade.Id,
            EntidadeAlvo = "Atividade",
            Gatilho = "Concluido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (atividade is null) return false;

        atividade.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = atividade.EmpresaId,
            RegistroId = atividade.Id,
            EntidadeAlvo = "Atividade",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public IQueryable<object> ObterTipos()
    {
        var tipos = Enum.GetNames<TipoAtividade>()
            .Select((nome, i) => new { Id = i, Nome = nome })
            .AsQueryable();
        return tipos;
    }
}

using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class AtividadeServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IAtividadeServico
{
    // Retorna a entidade (e não DTO) para casar com o EDM "Atividades" e permitir $expand/$select/$count.
    public IQueryable<Atividade> ObterTodos()
        => db.Atividades.AsNoTracking();

    public async Task<Atividade?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Atividades.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Atividade> CriarAsync(Delta<Atividade> delta, CancellationToken ct = default)
    {
        var atividade = new Atividade { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(atividade);
        atividade.EmpresaId = db.EmpresaIdAtual;
        atividade.ResponsavelId ??= db.UsuarioIdAtual;
        await ValidarResponsavelAsync(atividade.ResponsavelId, ct);

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

        return atividade;
    }

    public async Task<Atividade?> AtualizarAsync(long id, Delta<Atividade> delta, CancellationToken ct = default)
    {
        var atividade = await db.Atividades.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (atividade is null) return null;

        var empresaId = atividade.EmpresaId;
        var responsavelAnterior = atividade.ResponsavelId;
        delta.Patch(atividade);
        atividade.EmpresaId = empresaId; // o tenant nunca muda via PATCH
        if (atividade.ResponsavelId != responsavelAnterior)
        {
            try { await ValidarResponsavelAsync(atividade.ResponsavelId, ct); }
            catch { await db.Entry(atividade).ReloadAsync(ct); throw; }
        }
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = atividade.EmpresaId,
            RegistroId = atividade.Id,
            EntidadeAlvo = "Atividade",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return atividade;
    }

    /// <summary>O responsável precisa ser um usuário ativo da mesma empresa (filtro global do EF).</summary>
    private async Task ValidarResponsavelAsync(long? responsavelId, CancellationToken ct)
    {
        if (responsavelId is null) return;
        if (!await db.Usuarios.AnyAsync(u => u.Id == responsavelId, ct))
            throw new RegraNegocioException("O responsável informado não existe.");
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

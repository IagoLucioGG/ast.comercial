using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class EquipeServico(AppDbContext db) : IEquipeServico
{
    public IQueryable<Equipe> ObterTodos()
        => db.Equipes.AsNoTracking();

    public async Task<Equipe?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Equipes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Equipe> CriarAsync(Delta<Equipe> delta, CancellationToken ct = default)
    {
        var equipe = new Equipe();
        delta.Patch(equipe);
        equipe.Id = 0;
        equipe.EmpresaId = db.EmpresaIdAtual;
        equipe.Ativo = true;
        equipe.ChaveExterna = null;
        equipe.Membros = [];
        await ValidarAsync(equipe, ct);
        db.Equipes.Add(equipe);
        await db.SaveChangesAsync(ct);
        return equipe;
    }

    public async Task<Equipe?> AtualizarAsync(long id, Delta<Equipe> delta, CancellationToken ct = default)
    {
        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (equipe is null) return null;
        var (empresaId, criadoEm, ativo, chave) = (equipe.EmpresaId, equipe.CriadoEm, equipe.Ativo, equipe.ChaveExterna);
        delta.Patch(equipe);
        equipe.Id = id;
        equipe.EmpresaId = empresaId;
        equipe.CriadoEm = criadoEm;
        equipe.Ativo = ativo;
        equipe.ChaveExterna = chave;
        await ValidarAsync(equipe, ct);
        equipe.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return equipe;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (equipe is null) return false;
        equipe.Ativo = false;
        equipe.AtualizadoEm = DateTime.UtcNow;
        // Desvincula os membros junto com a equipe.
        var membros = await db.MembrosEquipe.Where(m => m.EquipeId == id).ToListAsync(ct);
        foreach (var m in membros) { m.Ativo = false; m.AtualizadoEm = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<MembroEquipe> ObterMembros()
        => db.MembrosEquipe.AsNoTracking();

    public async Task<MembroEquipe> CriarMembroAsync(long equipeId, long usuarioId, PapelEquipe papel, CancellationToken ct = default)
    {
        var entidade = new MembroEquipe { EquipeId = equipeId, UsuarioId = usuarioId, Papel = papel, EmpresaId = db.EmpresaIdAtual };

        if (!Enum.IsDefined(entidade.Papel))
            throw new RegraNegocioException("Papel inválido.");
        if (!await db.Equipes.AnyAsync(e => e.Id == entidade.EquipeId, ct))
            throw new RegraNegocioException("Equipe não encontrada.");
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == entidade.UsuarioId, ct)
            ?? throw new RegraNegocioException("Usuário não encontrado.");
        if (usuario.Tipo != TipoUsuario.Normal)
            throw new RegraNegocioException("Usuários de integração não podem ser membros de equipes.");
        var existente = await db.MembrosEquipe.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.EmpresaId == db.EmpresaIdAtual && m.EquipeId == entidade.EquipeId && m.UsuarioId == entidade.UsuarioId, ct);
        if (existente is not null)
        {
            if (existente.Ativo) throw new RegraNegocioException("Este usuário já faz parte da equipe.");
            // Vínculo removido logicamente: reativa em vez de duplicar (índice único).
            existente.Ativo = true;
            existente.Papel = entidade.Papel;
            existente.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return existente;
        }

        db.MembrosEquipe.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<MembroEquipe?> AtualizarMembroAsync(long id, PapelEquipe papel, CancellationToken ct = default)
    {
        var membro = await db.MembrosEquipe.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (membro is null) return null;
        if (!Enum.IsDefined(papel))
            throw new RegraNegocioException("Papel inválido.");
        membro.Papel = papel;
        membro.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return membro;
    }

    public async Task<bool> RemoverMembroAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.MembrosEquipe.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        entidade.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidarAsync(Equipe equipe, CancellationToken ct)
    {
        equipe.Nome = (equipe.Nome ?? "").Trim();
        if (equipe.Nome.Length == 0) throw new RegraNegocioException("O nome da equipe é obrigatório.");
        if (equipe.Nome.Length > 150) throw new RegraNegocioException("O nome da equipe deve ter no máximo 150 caracteres.");
        equipe.Descricao = string.IsNullOrWhiteSpace(equipe.Descricao) ? null : equipe.Descricao.Trim();
        if (equipe.Descricao is { Length: > 300 }) throw new RegraNegocioException("A descrição deve ter no máximo 300 caracteres.");
        var nome = equipe.Nome.ToLower();
        if (await db.Equipes.IgnoreQueryFilters().AnyAsync(e => e.EmpresaId == db.EmpresaIdAtual && e.Id != equipe.Id && e.Nome.ToLower() == nome, ct))
            throw new RegraNegocioException("Já existe uma equipe com este nome (ou uma equipe excluída com o mesmo nome).");
    }
}

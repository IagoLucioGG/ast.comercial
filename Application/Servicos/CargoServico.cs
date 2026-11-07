using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class CargoServico(AppDbContext db) : ICargoServico
{
    public IQueryable<Cargo> ObterTodos()
        => db.Cargos.AsNoTracking();

    public async Task<Cargo?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Cargos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Cargo> CriarAsync(Delta<Cargo> delta, CancellationToken ct = default)
    {
        var cargo = new Cargo();
        delta.Patch(cargo);
        cargo.Id = 0;
        cargo.EmpresaId = db.EmpresaIdAtual;
        cargo.Ativo = true;
        cargo.ChaveExterna = null;
        await ValidarAsync(cargo, ct);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync(ct);
        return cargo;
    }

    public async Task<Cargo?> AtualizarAsync(long id, Delta<Cargo> delta, CancellationToken ct = default)
    {
        var cargo = await db.Cargos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cargo is null) return null;
        var (empresaId, criadoEm, ativo, chave) = (cargo.EmpresaId, cargo.CriadoEm, cargo.Ativo, cargo.ChaveExterna);
        delta.Patch(cargo);
        cargo.Id = id;
        cargo.EmpresaId = empresaId;
        cargo.CriadoEm = criadoEm;
        cargo.Ativo = ativo;
        cargo.ChaveExterna = chave;
        await ValidarAsync(cargo, ct);
        cargo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return cargo;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var cargo = await db.Cargos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cargo is null) return false;
        if (await db.Usuarios.AnyAsync(u => u.CargoId == id, ct))
            throw new RegraNegocioException("Há usuários ativos neste cargo. Mude o cargo deles antes de excluir.");
        cargo.Ativo = false;
        cargo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidarAsync(Cargo cargo, CancellationToken ct)
    {
        cargo.Nome = (cargo.Nome ?? "").Trim();
        if (cargo.Nome.Length == 0) throw new RegraNegocioException("O nome do cargo é obrigatório.");
        if (cargo.Nome.Length > 100) throw new RegraNegocioException("O nome do cargo deve ter no máximo 100 caracteres.");
        cargo.Descricao = string.IsNullOrWhiteSpace(cargo.Descricao) ? null : cargo.Descricao.Trim();
        if (cargo.Descricao is { Length: > 300 }) throw new RegraNegocioException("A descrição deve ter no máximo 300 caracteres.");
        if (cargo.Nivel < 0 || cargo.Nivel > 100) throw new RegraNegocioException("O nível deve estar entre 0 e 100.");
        var nome = cargo.Nome.ToLower();
        if (await db.Cargos.IgnoreQueryFilters().AnyAsync(c => c.EmpresaId == db.EmpresaIdAtual && c.Id != cargo.Id && c.Nome.ToLower() == nome, ct))
            throw new RegraNegocioException("Já existe um cargo com este nome (ou um cargo excluído com o mesmo nome).");
    }
}

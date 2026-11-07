using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class DepartamentoServico(AppDbContext db) : IDepartamentoServico
{
    public IQueryable<Departamento> ObterTodos()
        => db.Departamentos.AsNoTracking();

    public async Task<Departamento?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Departamento> CriarAsync(Delta<Departamento> delta, CancellationToken ct = default)
    {
        var dep = new Departamento();
        delta.Patch(dep);
        dep.Id = 0;
        dep.EmpresaId = db.EmpresaIdAtual;
        dep.Ativo = true;
        dep.ChaveExterna = null;
        dep.DepartamentoPai = null;
        await ValidarAsync(dep, ct);
        db.Departamentos.Add(dep);
        await db.SaveChangesAsync(ct);
        return dep;
    }

    public async Task<Departamento?> AtualizarAsync(long id, Delta<Departamento> delta, CancellationToken ct = default)
    {
        var dep = await db.Departamentos.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dep is null) return null;
        var (empresaId, criadoEm, ativo, chave) = (dep.EmpresaId, dep.CriadoEm, dep.Ativo, dep.ChaveExterna);
        delta.Patch(dep);
        dep.Id = id;
        dep.EmpresaId = empresaId;
        dep.CriadoEm = criadoEm;
        dep.Ativo = ativo;
        dep.ChaveExterna = chave;
        await ValidarAsync(dep, ct);
        dep.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return dep;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var dep = await db.Departamentos.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dep is null) return false;
        if (await db.Usuarios.AnyAsync(u => u.DepartamentoId == id, ct))
            throw new RegraNegocioException("Há usuários ativos neste departamento. Mude o departamento deles antes de excluir.");
        if (await db.Departamentos.AnyAsync(d => d.DepartamentoPaiId == id, ct))
            throw new RegraNegocioException("Este departamento possui subdepartamentos ativos. Exclua ou mova-os antes.");
        dep.Ativo = false;
        dep.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidarAsync(Departamento dep, CancellationToken ct)
    {
        dep.Nome = (dep.Nome ?? "").Trim();
        if (dep.Nome.Length == 0) throw new RegraNegocioException("O nome do departamento é obrigatório.");
        if (dep.Nome.Length > 150) throw new RegraNegocioException("O nome do departamento deve ter no máximo 150 caracteres.");
        dep.Descricao = string.IsNullOrWhiteSpace(dep.Descricao) ? null : dep.Descricao.Trim();
        if (dep.Descricao is { Length: > 300 }) throw new RegraNegocioException("A descrição deve ter no máximo 300 caracteres.");

        if (dep.DepartamentoPaiId is long paiId)
        {
            if (dep.Id != 0 && paiId == dep.Id)
                throw new RegraNegocioException("Um departamento não pode ser pai dele mesmo.");
            if (!await db.Departamentos.AnyAsync(d => d.Id == paiId, ct))
                throw new RegraNegocioException("Departamento pai não encontrado.");
            // Evita ciclo: sobe a cadeia de pais do pai escolhido.
            var atual = (long?)paiId;
            var visitados = 0;
            while (atual is long a && visitados++ < 50)
            {
                if (dep.Id != 0 && a == dep.Id)
                    throw new RegraNegocioException("O departamento pai escolhido criaria um ciclo na hierarquia.");
                atual = await db.Departamentos.Where(d => d.Id == a).Select(d => d.DepartamentoPaiId).FirstOrDefaultAsync(ct);
            }
        }

        var nome = dep.Nome.ToLower();
        if (await db.Departamentos.IgnoreQueryFilters().AnyAsync(d => d.EmpresaId == db.EmpresaIdAtual && d.Id != dep.Id && d.Nome.ToLower() == nome, ct))
            throw new RegraNegocioException("Já existe um departamento com este nome (ou um departamento excluído com o mesmo nome).");
    }
}

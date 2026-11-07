using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class EmpresaServico(AppDbContext db) : IEmpresaServico
{
    // Empresa não tem filtro global de tenant: restringe sempre à empresa do usuário logado.
    public IQueryable<Empresa> ObterTodos()
        => db.Empresas.AsNoTracking().Where(e => e.Id == db.EmpresaIdAtual);

    public async Task<Empresa?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.Id == db.EmpresaIdAtual, ct);

    public async Task<Empresa?> AtualizarAsync(long id, Delta<Empresa> delta, CancellationToken ct = default)
    {
        // Só a própria empresa pode ser alterada.
        if (id != db.EmpresaIdAtual) return null;
        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (empresa is null) return null;

        var (idOriginal, empresaIdOriginal, criadoEm, ativo, chave) =
            (empresa.Id, empresa.EmpresaId, empresa.CriadoEm, empresa.Ativo, empresa.ChaveExterna);

        delta.Patch(empresa);

        // Campos de controle não podem ser alterados pelo cliente.
        empresa.Id = idOriginal;
        empresa.EmpresaId = empresaIdOriginal;
        empresa.CriadoEm = criadoEm;
        empresa.Ativo = ativo;
        empresa.ChaveExterna = chave;

        empresa.Nome = (empresa.Nome ?? "").Trim();
        if (empresa.Nome.Length == 0) throw new RegraNegocioException("O nome da empresa é obrigatório.");
        if (empresa.Nome.Length > 200) throw new RegraNegocioException("O nome da empresa deve ter no máximo 200 caracteres.");
        empresa.RazaoSocial = Limpar(empresa.RazaoSocial, 200, "Razão social");
        empresa.Cnpj = Limpar(empresa.Cnpj, 20, "CNPJ");
        empresa.Telefone = Limpar(empresa.Telefone, 30, "Telefone");
        empresa.Email = Limpar(empresa.Email, 150, "E-mail");
        empresa.Endereco = Limpar(empresa.Endereco, 300, "Endereço");
        empresa.Cidade = Limpar(empresa.Cidade, 100, "Cidade");
        empresa.Estado = Limpar(empresa.Estado, 50, "Estado");
        empresa.Cep = Limpar(empresa.Cep, 15, "CEP");
        empresa.LogoUrl = Limpar(empresa.LogoUrl, 500, "Logo");
        if (empresa.Email is not null && !empresa.Email.Contains('@'))
            throw new RegraNegocioException("E-mail da empresa inválido.");
        if (empresa.LogoUrl is not null && !(empresa.LogoUrl.StartsWith("https://") || empresa.LogoUrl.StartsWith("http://") || empresa.LogoUrl.StartsWith('/')))
            throw new RegraNegocioException("A URL da logo deve começar com http://, https:// ou /.");

        empresa.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return empresa;
    }

    private static string? Limpar(string? valor, int max, string campo)
    {
        var v = valor?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Length > max) throw new RegraNegocioException($"{campo} deve ter no máximo {max} caracteres.");
        return v;
    }
}

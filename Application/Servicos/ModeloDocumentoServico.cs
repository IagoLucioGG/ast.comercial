using System.Text.RegularExpressions;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public partial class ModeloDocumentoServico(AppDbContext db) : IModeloDocumentoServico
{
    public IQueryable<ModeloDocumento> ObterTodos() => db.ModelosDocumento.AsNoTracking();

    public async Task<ModeloDocumento?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.ModelosDocumento.AsNoTracking()
            .Include(m => m.Secoes.OrderBy(s => s.Ordem))
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<ModeloDocumento> CriarAsync(Delta<ModeloDocumento> delta, CancellationToken ct = default)
    {
        var entidade = new ModeloDocumento { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.ModelosDocumento.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<ModeloDocumento?> AtualizarAsync(long id, Delta<ModeloDocumento> delta, CancellationToken ct = default)
    {
        var entidade = await db.ModelosDocumento.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.ModelosDocumento.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<SecaoModelo> ObterSecoes() => db.SecoesModelo.AsNoTracking();

    public async Task<SecaoModelo> CriarSecaoAsync(Delta<SecaoModelo> delta, CancellationToken ct = default)
    {
        var entidade = new SecaoModelo { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.SecoesModelo.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<SecaoModelo?> AtualizarSecaoAsync(long id, Delta<SecaoModelo> delta, CancellationToken ct = default)
    {
        var entidade = await db.SecoesModelo.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverSecaoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.SecoesModelo.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> RenderizarAsync(long modeloId, long registroId, CancellationToken ct = default)
    {
        var modelo = await db.ModelosDocumento.AsNoTracking()
            .Include(m => m.Secoes.OrderBy(s => s.Ordem))
            .FirstOrDefaultAsync(m => m.Id == modeloId, ct);

        if (modelo is null) return string.Empty;

        var proposta = await db.Propostas.AsNoTracking()
            .Include(p => p.Produtos)
            .Include(p => p.Secoes)
            .FirstOrDefaultAsync(p => p.Id == registroId, ct);

        if (proposta is null) return string.Empty;

        var cliente = proposta.ClienteId.HasValue
            ? await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == proposta.ClienteId, ct)
            : null;

        var contexto = new Dictionary<string, object?>
        {
            ["$proposta"] = proposta,
            ["$cliente"] = cliente,
            ["$empresa"] = await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == db.EmpresaIdAtual, ct),
        };

        var htmlFinal = new System.Text.StringBuilder();
        foreach (var secao in modelo.Secoes)
        {
            var html = SubstituirPlaceholders(secao.ConteudoHtml, contexto);
            htmlFinal.Append(html);
        }

        return htmlFinal.ToString();
    }

    private static string SubstituirPlaceholders(string html, Dictionary<string, object?> contexto)
    {
        // Substituir tags <field key="$entidade.campo" ...>[entidade.campo]</field> pelo valor real
        return FieldRegex().Replace(html, match =>
        {
            var key = match.Groups[1].Value; // $proposta.Valor
            var partes = key.TrimStart('$').Split('.', 2);
            if (partes.Length != 2) return match.Value;

            var variavel = $"${partes[0]}";
            var campo = partes[1];

            if (contexto.TryGetValue(variavel, out var obj) && obj is not null)
            {
                var prop = obj.GetType().GetProperty(campo);
                if (prop is not null)
                {
                    var valor = prop.GetValue(obj);
                    return valor?.ToString() ?? "";
                }
            }
            return "";
        });
    }

    [GeneratedRegex(@"<field[^>]*key=""([^""]+)""[^>]*>.*?</field>", RegexOptions.Singleline)]
    private static partial Regex FieldRegex();
}

using System.Text.RegularExpressions;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

/// <summary>
/// CRUD das listas de apoio configuráveis pelo administrador (origens, ramos, portes, status de cliente,
/// motivos de perda, status de negócio, famílias e grupos de produto).
/// Rotas: odata/AdminCadastros/{tipo}[/{id}] (prefixo odata para reaproveitar o proxy e o cliente de API do front). JSON simples (camelCase), exclusão lógica, isolado por empresa
/// (o filtro global do EF já restringe por EmpresaId e Ativo).
/// Existe porque as rotas "@" antigas (Clientes@Origens etc.) recebem Delta fora do contexto OData:
/// o POST grava registro vazio e responde 400.
/// </summary>
[ApiController]
[Route("odata/AdminCadastros")]
public partial class CadastrosAuxiliaresController(AppDbContext db) : ControllerBase
{
    public class CadastroDto
    {
        public long Id { get; set; }
        public string? Nome { get; set; }
        public int? Ordem { get; set; }
        public string? Cor { get; set; }
        public string? Descricao { get; set; }
        public bool? Padrao { get; set; }
        /// <summary>StatusNegocio: Aberto | Ganho | Perdido.</summary>
        public string? TipoBase { get; set; }
        /// <summary>GrupoProduto: família (0 limpa o vínculo no PATCH).</summary>
        public long? FamiliaProdutoId { get; set; }
        public bool Ativo { get; set; } = true;
    }

    private static readonly string[] Tipos =
    [
        "origens", "ramos", "portes", "status-cliente", "motivos-perda",
        "status-negocio", "familias-produto", "grupos-produto",
    ];

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex CorRegex();

    [HttpGet("tipos")]
    public ActionResult ListarTipos() => Ok(Tipos);

    [HttpGet("{tipo}")]
    public async Task<ActionResult> Listar(string tipo, CancellationToken ct)
    {
        return tipo switch
        {
            "origens" => Ok(await db.OrigensContato.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Descricao = x.Descricao, Ativo = x.Ativo }).ToListAsync(ct)),
            "ramos" => Ok(await db.RamosAtividade.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Ativo = x.Ativo }).ToListAsync(ct)),
            "portes" => Ok(await db.PortesEmpresa.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Ativo = x.Ativo }).ToListAsync(ct)),
            "status-cliente" => Ok(await db.StatusClientes.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Cor = x.Cor, Padrao = x.Padrao, Ativo = x.Ativo }).ToListAsync(ct)),
            "motivos-perda" => Ok(await db.MotivosPerda.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Ativo = x.Ativo }).ToListAsync(ct)),
            "status-negocio" => Ok((await db.StatusNegocios.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome).ToListAsync(ct))
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Cor = x.Cor, Padrao = x.Padrao, TipoBase = x.TipoBase.ToString(), Ativo = x.Ativo })),
            "familias-produto" => Ok(await db.FamiliasProduto.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, Ativo = x.Ativo }).ToListAsync(ct)),
            "grupos-produto" => Ok(await db.GruposProduto.AsNoTracking().OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
                .Select(x => new CadastroDto { Id = x.Id, Nome = x.Nome, Ordem = x.Ordem, FamiliaProdutoId = x.FamiliaProdutoId, Ativo = x.Ativo }).ToListAsync(ct)),
            _ => NotFound(new { erro = "Tipo de cadastro desconhecido.", status = 404 }),
        };
    }

    [HttpPost("{tipo}")]
    public async Task<ActionResult> Criar(string tipo, [FromBody] CadastroDto dto, CancellationToken ct)
    {
        if (!Tipos.Contains(tipo)) return NotFound(new { erro = "Tipo de cadastro desconhecido.", status = 404 });
        var nome = LimparNome(dto.Nome);
        var ordem = ValidarOrdem(dto.Ordem ?? 0);
        var cor = ValidarCor(dto.Cor);
        var empresaId = db.EmpresaIdAtual;

        switch (tipo)
        {
            case "origens":
                var o = new OrigemContato { EmpresaId = empresaId, Nome = nome, Ordem = ordem, Descricao = LimparTexto(dto.Descricao, 300) };
                db.OrigensContato.Add(o); await db.SaveChangesAsync(ct); return Ok(new CadastroDto { Id = o.Id, Nome = o.Nome, Ordem = o.Ordem, Descricao = o.Descricao, Ativo = true });
            case "ramos":
                var r = new RamoAtividade { EmpresaId = empresaId, Nome = nome, Ordem = ordem };
                db.RamosAtividade.Add(r); await db.SaveChangesAsync(ct); return Ok(Simples(r.Id, r.Nome, r.Ordem));
            case "portes":
                var p = new PorteEmpresa { EmpresaId = empresaId, Nome = nome, Ordem = ordem };
                db.PortesEmpresa.Add(p); await db.SaveChangesAsync(ct); return Ok(Simples(p.Id, p.Nome, p.Ordem));
            case "status-cliente":
                var sc = new StatusCliente { EmpresaId = empresaId, Nome = nome, Ordem = ordem, Cor = cor, Padrao = dto.Padrao ?? false };
                if (sc.Padrao) await DesmarcarPadraoAsync(db.StatusClientes, 0, ct);
                db.StatusClientes.Add(sc); await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = sc.Id, Nome = sc.Nome, Ordem = sc.Ordem, Cor = sc.Cor, Padrao = sc.Padrao, Ativo = true });
            case "motivos-perda":
                var m = new MotivoPerda { EmpresaId = empresaId, Nome = nome, Ordem = ordem };
                db.MotivosPerda.Add(m); await db.SaveChangesAsync(ct); return Ok(Simples(m.Id, m.Nome, m.Ordem));
            case "status-negocio":
                var sn = new StatusNegocio { EmpresaId = empresaId, Nome = nome, Ordem = ordem, Cor = cor, Padrao = dto.Padrao ?? false, TipoBase = ParseTipoBase(dto.TipoBase, TipoStatusNegocio.Aberto) };
                if (sn.Padrao) await DesmarcarPadraoAsync(db.StatusNegocios, 0, ct);
                db.StatusNegocios.Add(sn); await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = sn.Id, Nome = sn.Nome, Ordem = sn.Ordem, Cor = sn.Cor, Padrao = sn.Padrao, TipoBase = sn.TipoBase.ToString(), Ativo = true });
            case "familias-produto":
                var f = new FamiliaProduto { EmpresaId = empresaId, Nome = nome, Ordem = ordem };
                db.FamiliasProduto.Add(f); await db.SaveChangesAsync(ct); return Ok(Simples(f.Id, f.Nome, f.Ordem));
            default: // grupos-produto
                var familia = await ValidarFamiliaAsync(dto.FamiliaProdutoId, ct);
                var g = new GrupoProduto { EmpresaId = empresaId, Nome = nome, Ordem = ordem, FamiliaProdutoId = familia };
                db.GruposProduto.Add(g); await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = g.Id, Nome = g.Nome, Ordem = g.Ordem, FamiliaProdutoId = g.FamiliaProdutoId, Ativo = true });
        }
    }

    [HttpPatch("{tipo}/{id:long}")]
    public async Task<ActionResult> Atualizar(string tipo, long id, [FromBody] CadastroDto dto, CancellationToken ct)
    {
        if (!Tipos.Contains(tipo)) return NotFound(new { erro = "Tipo de cadastro desconhecido.", status = 404 });
        var nome = dto.Nome is null ? null : LimparNome(dto.Nome);
        var ordem = dto.Ordem is null ? (int?)null : ValidarOrdem(dto.Ordem.Value);
        var cor = ValidarCor(dto.Cor);
        var agora = DateTime.UtcNow;

        switch (tipo)
        {
            case "origens":
                var o = await db.OrigensContato.FirstOrDefaultAsync(x => x.Id == id, ct); if (o is null) return NotFound();
                if (nome is not null) o.Nome = nome; if (ordem is not null) o.Ordem = ordem.Value;
                if (dto.Descricao is not null) o.Descricao = LimparTexto(dto.Descricao, 300);
                o.AtualizadoEm = agora; await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = o.Id, Nome = o.Nome, Ordem = o.Ordem, Descricao = o.Descricao, Ativo = o.Ativo });
            case "ramos":
                var r = await db.RamosAtividade.FirstOrDefaultAsync(x => x.Id == id, ct); if (r is null) return NotFound();
                if (nome is not null) r.Nome = nome; if (ordem is not null) r.Ordem = ordem.Value;
                r.AtualizadoEm = agora; await db.SaveChangesAsync(ct); return Ok(Simples(r.Id, r.Nome, r.Ordem));
            case "portes":
                var p = await db.PortesEmpresa.FirstOrDefaultAsync(x => x.Id == id, ct); if (p is null) return NotFound();
                if (nome is not null) p.Nome = nome; if (ordem is not null) p.Ordem = ordem.Value;
                p.AtualizadoEm = agora; await db.SaveChangesAsync(ct); return Ok(Simples(p.Id, p.Nome, p.Ordem));
            case "status-cliente":
                var sc = await db.StatusClientes.FirstOrDefaultAsync(x => x.Id == id, ct); if (sc is null) return NotFound();
                if (nome is not null) sc.Nome = nome; if (ordem is not null) sc.Ordem = ordem.Value;
                if (dto.Cor is not null) sc.Cor = cor;
                if (dto.Padrao is bool padSc) { if (padSc) await DesmarcarPadraoAsync(db.StatusClientes, id, ct); sc.Padrao = padSc; }
                sc.AtualizadoEm = agora; await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = sc.Id, Nome = sc.Nome, Ordem = sc.Ordem, Cor = sc.Cor, Padrao = sc.Padrao, Ativo = sc.Ativo });
            case "motivos-perda":
                var m = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct); if (m is null) return NotFound();
                if (nome is not null) m.Nome = nome; if (ordem is not null) m.Ordem = ordem.Value;
                m.AtualizadoEm = agora; await db.SaveChangesAsync(ct); return Ok(Simples(m.Id, m.Nome, m.Ordem));
            case "status-negocio":
                var sn = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct); if (sn is null) return NotFound();
                if (nome is not null) sn.Nome = nome; if (ordem is not null) sn.Ordem = ordem.Value;
                if (dto.Cor is not null) sn.Cor = cor;
                if (dto.TipoBase is not null) sn.TipoBase = ParseTipoBase(dto.TipoBase, sn.TipoBase);
                if (dto.Padrao is bool padSn) { if (padSn) await DesmarcarPadraoAsync(db.StatusNegocios, id, ct); sn.Padrao = padSn; }
                sn.AtualizadoEm = agora; await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = sn.Id, Nome = sn.Nome, Ordem = sn.Ordem, Cor = sn.Cor, Padrao = sn.Padrao, TipoBase = sn.TipoBase.ToString(), Ativo = sn.Ativo });
            case "familias-produto":
                var f = await db.FamiliasProduto.FirstOrDefaultAsync(x => x.Id == id, ct); if (f is null) return NotFound();
                if (nome is not null) f.Nome = nome; if (ordem is not null) f.Ordem = ordem.Value;
                f.AtualizadoEm = agora; await db.SaveChangesAsync(ct); return Ok(Simples(f.Id, f.Nome, f.Ordem));
            default:
                var g = await db.GruposProduto.FirstOrDefaultAsync(x => x.Id == id, ct); if (g is null) return NotFound();
                if (nome is not null) g.Nome = nome; if (ordem is not null) g.Ordem = ordem.Value;
                if (dto.FamiliaProdutoId is long fam) g.FamiliaProdutoId = fam == 0 ? null : await ValidarFamiliaAsync(fam, ct);
                g.AtualizadoEm = agora; await db.SaveChangesAsync(ct);
                return Ok(new CadastroDto { Id = g.Id, Nome = g.Nome, Ordem = g.Ordem, FamiliaProdutoId = g.FamiliaProdutoId, Ativo = g.Ativo });
        }
    }

    [HttpDelete("{tipo}/{id:long}")]
    public async Task<ActionResult> Remover(string tipo, long id, CancellationToken ct)
    {
        if (!Tipos.Contains(tipo)) return NotFound(new { erro = "Tipo de cadastro desconhecido.", status = 404 });
        EntidadeBase? e;
        switch (tipo)
        {
            case "origens": e = await db.OrigensContato.FirstOrDefaultAsync(x => x.Id == id, ct); break;
            case "ramos": e = await db.RamosAtividade.FirstOrDefaultAsync(x => x.Id == id, ct); break;
            case "portes": e = await db.PortesEmpresa.FirstOrDefaultAsync(x => x.Id == id, ct); break;
            case "status-cliente":
                var sc = await db.StatusClientes.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (sc is { Padrao: true }) throw new AST.Comercial.Application.RegraNegocioException("O status padrão não pode ser excluído. Defina outro como padrão antes.");
                e = sc; break;
            case "motivos-perda": e = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct); break;
            case "status-negocio":
                var sn = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (sn is { Padrao: true }) throw new AST.Comercial.Application.RegraNegocioException("O status padrão não pode ser excluído. Defina outro como padrão antes.");
                e = sn; break;
            case "familias-produto":
                if (await db.GruposProduto.AnyAsync(x => x.FamiliaProdutoId == id, ct))
                    throw new AST.Comercial.Application.RegraNegocioException("Esta família tem grupos ativos. Exclua ou mova os grupos antes.");
                e = await db.FamiliasProduto.FirstOrDefaultAsync(x => x.Id == id, ct); break;
            default: e = await db.GruposProduto.FirstOrDefaultAsync(x => x.Id == id, ct); break;
        }
        if (e is null) return NotFound();
        e.Ativo = false;
        e.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------------------------------------------------------------- helpers

    private static CadastroDto Simples(long id, string nome, int ordem) => new() { Id = id, Nome = nome, Ordem = ordem, Ativo = true };

    private static string LimparNome(string? nome)
    {
        var n = (nome ?? "").Trim();
        if (n.Length == 0) throw new AST.Comercial.Application.RegraNegocioException("O nome é obrigatório.");
        if (n.Length > 150) throw new AST.Comercial.Application.RegraNegocioException("O nome deve ter no máximo 150 caracteres.");
        return n;
    }

    private static string? LimparTexto(string? texto, int max)
    {
        var t = texto?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.Length > max) throw new AST.Comercial.Application.RegraNegocioException($"O texto deve ter no máximo {max} caracteres.");
        return t;
    }

    private static int ValidarOrdem(int ordem)
    {
        if (ordem < 0 || ordem > 100000) throw new AST.Comercial.Application.RegraNegocioException("A ordem deve estar entre 0 e 100000.");
        return ordem;
    }

    private static string? ValidarCor(string? cor)
    {
        var c = cor?.Trim();
        if (string.IsNullOrEmpty(c)) return null;
        if (!CorRegex().IsMatch(c)) throw new AST.Comercial.Application.RegraNegocioException("Cor inválida. Use o formato #RRGGBB.");
        return c;
    }

    private static TipoStatusNegocio ParseTipoBase(string? valor, TipoStatusNegocio padrao)
    {
        if (string.IsNullOrWhiteSpace(valor)) return padrao;
        if (Enum.TryParse<TipoStatusNegocio>(valor.Trim(), true, out var t) && Enum.IsDefined(t)) return t;
        throw new AST.Comercial.Application.RegraNegocioException("Tipo base inválido. Use Aberto, Ganho ou Perdido.");
    }

    private async Task<long?> ValidarFamiliaAsync(long? familiaId, CancellationToken ct)
    {
        if (familiaId is null or 0) return null;
        if (!await db.FamiliasProduto.AnyAsync(f => f.Id == familiaId, ct))
            throw new AST.Comercial.Application.RegraNegocioException("Família de produto não encontrada.");
        return familiaId;
    }

    private async Task DesmarcarPadraoAsync<T>(DbSet<T> set, long exceto, CancellationToken ct) where T : EntidadeBase
    {
        var outros = await set.Where(x => x.Id != exceto && EF.Property<bool>(x, "Padrao")).ToListAsync(ct);
        foreach (var x in outros) { typeof(T).GetProperty("Padrao")!.SetValue(x, false); x.AtualizadoEm = DateTime.UtcNow; }
    }
}

using System.Text.RegularExpressions;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

/// <summary>
/// Importa o modelo de documento da Ploomes automaticamente.
/// Lê HTMLs da pasta "HTML ploomes/", extrai campos, cria no sistema e monta o modelo.
/// POST /api/importar-modelo-ploomes
/// </summary>
[ApiController]
[Route("api/importar-modelo-ploomes")]
public partial class ImportarModeloPloomesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Importar(CancellationToken ct)
    {
        var empresaId = db.EmpresaIdAtual;
        if (empresaId == 0) return Unauthorized();

        var modeloExistente = await db.ModelosDocumento.AnyAsync(m => m.Nome == "Proposta Comercial 3.0", ct);
        if (modeloExistente) return Ok(new { mensagem = "Modelo já existe." });

        var pastaHtml = Path.Combine(env.ContentRootPath, "HTML ploomes");
        if (!Directory.Exists(pastaHtml)) return BadRequest(new { erro = "Pasta 'HTML ploomes' não encontrada." });

        var arquivos = new[] { "Capa.html", "Secao1.html", "secao2.html", "secao3.html" };
        var secoesHtml = new List<(string Nome, string Html)>();
        foreach (var arq in arquivos)
        {
            var caminho = Path.Combine(pastaHtml, arq);
            if (!System.IO.File.Exists(caminho)) continue;
            var html = await System.IO.File.ReadAllTextAsync(caminho, ct);
            var nome = arq.Replace(".html", "")
                .Replace("Capa", "Capa")
                .Replace("Secao1", "Seção 1")
                .Replace("secao2", "Seção 2")
                .Replace("secao3", "Seção 3");
            secoesHtml.Add((nome, html));
        }

        var camposExtraidos = new HashSet<(string Entidade, string Campo)>();
        var secoesConvertidas = new List<(string Nome, string Html)>();
        foreach (var (nome, html) in secoesHtml)
        {
            var convertido = ConverterHtml(html, camposExtraidos);
            secoesConvertidas.Add((nome, convertido));
        }

        // Criar campos que não existem
        var nomesExistentes = new HashSet<string>(
            await db.Campos.Select(c => c.Nome.ToLower()).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var camposCriados = 0;
        foreach (var (entidade, campo) in camposExtraidos)
        {
            if (nomesExistentes.Contains(campo)) continue;
            var entAlvo = MapearEntidade(entidade);
            var novo = new Campo { Nome = campo, Tipo = TipoCampo.Texto, EntidadeAlvo = entAlvo, EmpresaId = empresaId, Visivel = true, Ativo = true };
            novo.GerarChave();
            db.Campos.Add(novo);
            nomesExistentes.Add(campo);
            camposCriados++;
        }
        if (camposCriados > 0) await db.SaveChangesAsync(ct);

        // Criar modelo
        var modelo = new ModeloDocumento { Nome = "Proposta Comercial 3.0", EntidadeAlvo = EntidadeAlvo.Proposta, Padrao = true, Ordem = 1, Ativo = true, EmpresaId = empresaId };
        db.ModelosDocumento.Add(modelo);
        await db.SaveChangesAsync(ct);

        var ordem = 0;
        foreach (var (nome, html) in secoesConvertidas)
        {
            db.SecoesModelo.Add(new SecaoModelo { Nome = nome, Ordem = ordem++, ConteudoHtml = html, ModeloDocumentoId = modelo.Id, EmpresaId = empresaId, Ativo = true });
        }
        await db.SaveChangesAsync(ct);

        return Ok(new { mensagem = $"Modelo criado com {secoesConvertidas.Count} seções e {camposCriados} campos.", modeloId = modelo.Id });
    }

    private static EntidadeAlvo MapearEntidade(string entidade) => entidade.ToLower() switch
    {
        "proposta" => EntidadeAlvo.Proposta,
        "cliente" => EntidadeAlvo.Cliente,
        "produto da proposta" or "produto da proposta / produto" => EntidadeAlvo.ItemProposta,
        "bloco" => EntidadeAlvo.SecaoProposta,
        "vínculo de produto da proposta" or "vínculo de produto da proposta / produto" => EntidadeAlvo.ItemProposta,
        "criador" or "perfil de usuário" => EntidadeAlvo.Usuario,
        _ => EntidadeAlvo.Proposta
    };

    private static string ConverterHtml(string html, HashSet<(string Entidade, string Campo)> campos)
    {
        html = FieldTagRegex().Replace(html, match =>
        {
            var conteudo = match.Groups[1].Value.Trim();
            var refMatch = RefRegex().Match(conteudo);
            if (!refMatch.Success) return conteudo;

            var entidade = refMatch.Groups[1].Value.Trim();
            var campo = refMatch.Groups[2].Value.Trim();
            campos.Add((entidade, campo));

            var prefixo = entidade.ToLower() switch
            {
                "proposta" => "$proposta",
                "cliente" => "$cliente",
                "produto da proposta" => "$item",
                "produto da proposta / produto" => "$item.Produto",
                "bloco" => "$bloco",
                "vínculo de produto da proposta" => "$vinculo",
                "vínculo de produto da proposta / produto" => "$vinculo.Produto",
                "criador" => "$usuario",
                "perfil de usuário" => "$usuario.Perfil",
                _ => $"${entidade.ToLower().Replace(" ", "")}"
            };
            return $"{prefixo}.{campo}";
        });

        return html;
    }

    [GeneratedRegex(@"<field[^>]*>(.*?)</field>", RegexOptions.Singleline)]
    private static partial Regex FieldTagRegex();

    [GeneratedRegex(@"\[([^.\]]+(?:\s*/\s*[^.\]]+)?)\.([^\]]+)\]")]
    private static partial Regex RefRegex();
}

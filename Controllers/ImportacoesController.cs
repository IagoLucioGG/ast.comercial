using System.Text;
using AST.Comercial.Application.Importacoes;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace AST.Comercial.Controllers;

/// <summary>
/// Importação de dados por CSV (Cliente, PessoaContato, Produto, Negocio) em 3 passos: analisar, validar, executar.
/// O arquivo é enviado (multipart/form-data, campo "arquivo") em cada passo e processado em memória: nada é gravado em disco.
/// O mapeamento é um JSON {"&lt;índice da coluna, base 0&gt;": "&lt;chave do campo&gt;"}.
/// </summary>
[Route("api/importacoes")]
public class ImportacoesController(IImportacaoServico servico, AppDbContext db, IPermissaoServico permissoes) : ControllerBase
{
    private const long LimiteRequisicao = CsvLeitor.LimiteBytes + 512 * 1024; // arquivo + campos do formulário

    private static readonly HashSet<string> TiposAceitos = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv", "application/csv", "text/plain", "text/comma-separated-values",
        "application/vnd.ms-excel", "application/octet-stream", "application/x-csv", "text/x-csv"
    };

    private ObjectResult Erro(int status, string mensagem) => StatusCode(status, new { erro = mensagem, status });

    // ------------------------------------------------------------------ passos

    [HttpPost("analisar")]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteRequisicao)]
    public async Task<ActionResult> Analisar(CancellationToken ct)
    {
        var (entrada, falha) = await LerEntradaAsync(ct);
        if (falha is not null) return falha;
        try
        {
            return Ok(await servico.AnalisarAsync(entrada!.Entidade, entrada.NomeArquivo, entrada.Bytes, ct));
        }
        catch (ArquivoImportacaoException ex) { return Erro(ex.Status, ex.Message); }
    }

    [HttpPost("validar")]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteRequisicao)]
    public async Task<ActionResult> Validar(CancellationToken ct)
    {
        var (entrada, falha) = await LerEntradaAsync(ct);
        if (falha is not null) return falha;
        try
        {
            return Ok(await servico.ValidarAsync(
                entrada!.Entidade, entrada.Bytes, entrada.Mapeamento, entrada.Duplicidade, entrada.Chave, ct));
        }
        catch (ArquivoImportacaoException ex) { return Erro(ex.Status, ex.Message); }
    }

    /// <summary>Inicia a gravação em segundo plano e devolve 202 com a importação; acompanhe por GET api/importacoes/{id}.</summary>
    [HttpPost("executar")]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteRequisicao)]
    public async Task<ActionResult> Executar(CancellationToken ct)
    {
        var (entrada, falha) = await LerEntradaAsync(ct);
        if (falha is not null) return falha;

        // Gravar em Clientes/Contatos/Produtos/Negócios exige a permissão da própria entidade, além da de Importação.
        var negada = await ChecarPermissaoDestinoAsync(entrada!.Entidade, entrada.Duplicidade, ct);
        if (negada is not null) return negada;

        try
        {
            var importacao = await servico.ExecutarAsync(
                entrada.Entidade, entrada.NomeArquivo, entrada.Bytes, entrada.Mapeamento, entrada.Duplicidade, entrada.Chave, ct);
            return Accepted($"/api/importacoes/{importacao.Id}", importacao);
        }
        catch (ArquivoImportacaoException ex) { return Erro(ex.Status, ex.Message); }
    }

    // ------------------------------------------------------------------ histórico e relatórios

    [HttpGet]
    public async Task<ActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanho = 20, CancellationToken ct = default)
    {
        var (itens, total) = await servico.ListarAsync(pagina, tamanho, ct);
        return Ok(new { itens, total, pagina = Math.Max(1, pagina), tamanho = Math.Clamp(tamanho, 1, 100) });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Obter(long id, CancellationToken ct)
    {
        var dto = await servico.ObterAsync(id, ct);
        return dto is null ? Erro(404, "Importação não encontrada.") : Ok(dto);
    }

    [HttpGet("{id:long}/erros.csv")]
    public async Task<ActionResult> RelatorioErros(long id, CancellationToken ct)
    {
        var csv = await servico.GerarRelatorioErrosAsync(id, ct);
        if (csv is null) return Erro(404, "Importação não encontrada.");
        // Nome do arquivo montado aqui (nada vem do usuário).
        return File(new UTF8Encoding(false).GetBytes(csv), "text/csv; charset=utf-8", $"erros-importacao-{id}.csv");
    }

    [HttpGet("modelo/{entidade}.csv")]
    public async Task<ActionResult> Modelo(string entidade, CancellationToken ct)
    {
        var chave = CatalogoImportacao.ResolverEntidade(entidade);
        if (chave is null) return Erro(404, "Entidade não importável. Use clientes, contatos, produtos ou negocios.");
        var csv = await servico.GerarModeloAsync(chave, ct);
        var nome = chave switch
        {
            "Cliente" => "clientes",
            "PessoaContato" => "contatos",
            "Produto" => "produtos",
            _ => "negocios"
        };
        return File(new UTF8Encoding(false).GetBytes(csv), "text/csv; charset=utf-8", $"modelo-{nome}.csv");
    }

    // ------------------------------------------------------------------ entrada

    private sealed record Entrada(
        string Entidade, string NomeArquivo, byte[] Bytes, string? Mapeamento, string? Duplicidade, string? Chave);

    private async Task<(Entrada? Entrada, ActionResult? Falha)> LerEntradaAsync(CancellationToken ct)
    {
        if (!Request.HasFormContentType)
            return (null, Erro(422, "Envie o arquivo como multipart/form-data, no campo 'arquivo'."));

        // Recusa cedo quando o tamanho declarado já passa do limite (sem ler o corpo).
        if (Request.ContentLength is > LimiteRequisicao)
            return (null, Erro(413, "O arquivo excede o limite de 5 MB."));

        var limiteFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limiteFeature is { IsReadOnly: false }) limiteFeature.MaxRequestBodySize = LimiteRequisicao;

        IFormCollection form;
        try { form = await Request.ReadFormAsync(ct); }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        { return (null, Erro(413, "O arquivo excede o limite de 5 MB.")); }
        catch (InvalidDataException)
        { return (null, Erro(413, "O arquivo excede o limite de 5 MB.")); }
        catch (BadHttpRequestException)
        { return (null, Erro(422, "Não foi possível ler o arquivo enviado.")); }

        var entidadeTexto = form["entidade"].ToString();
        var entidade = CatalogoImportacao.ResolverEntidade(entidadeTexto);
        if (entidade is null)
            return (null, Erro(422, "Entidade inválida. Use Cliente, PessoaContato, Produto ou Negocio."));

        var arquivo = form.Files.GetFile("arquivo");
        if (arquivo is null || form.Files.Count != 1)
            return (null, Erro(422, "Envie exatamente um arquivo CSV no campo 'arquivo'."));
        if (arquivo.Length == 0)
            return (null, Erro(422, "O arquivo está vazio."));
        if (arquivo.Length > CsvLeitor.LimiteBytes)
            return (null, Erro(413, "O arquivo excede o limite de 5 MB."));
        if (!string.Equals(Path.GetExtension(arquivo.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return (null, Erro(422, "Formato não aceito. Envie um arquivo com extensão .csv."));
        if (!string.IsNullOrWhiteSpace(arquivo.ContentType) && !TiposAceitos.Contains(arquivo.ContentType.Split(';')[0].Trim()))
            return (null, Erro(422, "Tipo de arquivo não aceito. Envie um CSV."));

        using var ms = new MemoryStream((int)arquivo.Length);
        await using (var origem = arquivo.OpenReadStream()) await origem.CopyToAsync(ms, ct);

        return (new Entrada(
            entidade, arquivo.FileName, ms.ToArray(),
            form["mapeamento"].ToString(), form["duplicidade"].ToString(), form["chave"].ToString()), null);
    }

    private async Task<ActionResult?> ChecarPermissaoDestinoAsync(string entidade, string? duplicidade, CancellationToken ct)
    {
        if (db.PerfilAtual is null or PerfilUsuario.Administrador) return null;
        var matriz = await permissoes.ObterMatrizAsync(db.EmpresaIdAtual, db.PerfilAtual.Value, ct);
        matriz.TryGetValue(entidade, out var perm);
        perm ??= PermissaoEfetiva.Nenhuma;
        var atualiza = string.Equals(Normalizador.Chave(duplicidade), "atualizar", StringComparison.Ordinal);
        var rotulo = CatalogoImportacao.RotuloEntidade(entidade);
        if (!perm.Criar) return Erro(403, $"Sem permissão para criar {rotulo}.");
        if (atualiza && !perm.Editar) return Erro(403, $"Sem permissão para editar {rotulo}.");
        return null;
    }
}

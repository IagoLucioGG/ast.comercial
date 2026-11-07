using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Automacoes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Controllers;

public class AutomacoesController(IAutomacaoServico servico) : ODataController
{
    [EnableQuery]
    public IQueryable<Automacao> Get() => servico.ObterTodos();

    [EnableQuery]
    public async Task<ActionResult<Automacao>> Get(long key)
    {
        var resultado = await servico.ObterPorIdAsync(key);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [EnableQuery]
    [HttpGet("odata/Automacoes@Fila")]
    public IQueryable<FilaAutomacao> ObterFila() => servico.ObterFila();

    [EnableQuery]
    [HttpGet("odata/Automacoes@Logs")]
    public IQueryable<ExecucaoAutomacao> ObterLogs() => servico.ObterLogs();

    [HttpGet("odata/Automacoes@Gatilhos")]
    public ActionResult ObterGatilhos()
    {
        var valores = Enum.GetNames<TipoGatilho>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }

    [HttpGet("odata/Automacoes@Acoes")]
    public ActionResult ObterAcoes()
    {
        var valores = Enum.GetNames<TipoAcao>().Select((nome, i) => new { Id = i, Nome = nome });
        return Ok(valores);
    }
}

/// <summary>
/// API de automações com corpo aninhado (filtros + ações). Contrato: mapeamento/analise/07-contrato-automacoes.md
/// </summary>
[Route("api/automacoes")]
public class AutomacoesApiController(IAutomacaoServico servico) : ControllerBase
{
    private ObjectResult Erro(int status, string mensagem, IReadOnlyList<string>? erros = null)
        => StatusCode(status, erros is null
            ? new { erro = mensagem, status }
            : new { erro = mensagem, erros, status });

    [HttpGet]
    public async Task<ActionResult> Listar([FromQuery] string? entidadeAlvo, [FromQuery] string? gatilho, [FromQuery] bool? ativo,
        [FromQuery] string? busca, [FromQuery] int skip = 0, [FromQuery] int top = 50, CancellationToken ct = default)
    {
        try { return Ok(await servico.ListarAsync(entidadeAlvo, gatilho, ativo, busca, skip, top, ct)); }
        catch (ValidacaoAutomacaoException ex) { return Erro(422, ex.Message, ex.Erros); }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Obter(long id, CancellationToken ct)
    {
        var dto = await servico.ObterDtoAsync(id, ct);
        return dto is null ? Erro(404, "Automação não encontrada.") : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] AutomacaoEntradaDto? entrada, CancellationToken ct)
    {
        if (entrada is null) return Erro(400, "Corpo da requisição inválido.");
        try
        {
            var dto = await servico.CriarAsync(entrada, ct);
            return Created($"/api/automacoes/{dto.Id}", dto);
        }
        catch (ValidacaoAutomacaoException ex) { return Erro(422, ex.Message, ex.Erros); }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AutomacaoEntradaDto? entrada, CancellationToken ct)
    {
        if (entrada is null) return Erro(400, "Corpo da requisição inválido.");
        try
        {
            var dto = await servico.AtualizarAsync(id, entrada, ct);
            return dto is null ? Erro(404, "Automação não encontrada.") : Ok(dto);
        }
        catch (ValidacaoAutomacaoException ex) { return Erro(422, ex.Message, ex.Erros); }
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Remover(long id, CancellationToken ct)
        => await servico.RemoverAsync(id, ct) ? NoContent() : Erro(404, "Automação não encontrada.");

    [HttpPost("{id:long}/ativar")]
    public Task<ActionResult> Ativar(long id, CancellationToken ct) => DefinirAtivo(id, true, ct);

    [HttpPost("{id:long}/desativar")]
    public Task<ActionResult> Desativar(long id, CancellationToken ct) => DefinirAtivo(id, false, ct);

    private async Task<ActionResult> DefinirAtivo(long id, bool ativo, CancellationToken ct)
    {
        try
        {
            var dto = await servico.DefinirAtivoAsync(id, ativo, ct);
            return dto is null ? Erro(404, "Automação não encontrada.") : Ok(dto);
        }
        catch (ValidacaoAutomacaoException ex) { return Erro(422, ex.Message, ex.Erros); }
    }

    [HttpGet("{id:long}/execucoes")]
    public async Task<ActionResult> Execucoes(long id, [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int top = 50, CancellationToken ct = default)
    {
        try
        {
            var r = await servico.ListarExecucoesAsync(id, status, skip, top, ct);
            return r is null ? Erro(404, "Automação não encontrada.") : Ok(r);
        }
        catch (ValidacaoAutomacaoException ex) { return Erro(422, ex.Message, ex.Erros); }
    }

    [HttpGet("catalogo")]
    public ActionResult Catalogo()
    {
        var entidades = CatalogoAutomacao.EntidadesAlvo.Select(e => new
        {
            valor = e.ToString(),
            nome = CatalogoAutomacao.NomesEntidade[e],
            gatilhos = CatalogoAutomacao.Gatilhos.Where(g => g.Implementado && g.Entidades.Contains(e)).Select(g => g.Valor.ToString()),
            campos = CatalogoAutomacao.Campos(e).Select(c => new { nome = c.Nome, tipo = c.Tipo, opcoes = c.Opcoes, editavel = c.Editavel })
        });
        var gatilhos = CatalogoAutomacao.Gatilhos.Select(g => new
        {
            valor = g.Valor.ToString(), nome = g.Nome, implementado = g.Implementado,
            entidades = g.Entidades.Select(x => x.ToString()), observacao = g.Observacao
        });
        var acoes = CatalogoAutomacao.Acoes.Select(a => new
        {
            valor = a.Valor.ToString(), nome = a.Nome, implementado = a.Implementado,
            entidades = a.Entidades?.Select(x => x.ToString()),
            configuracao = a.Configuracao.Select(p => new { chave = p.Chave, tipo = p.Tipo, obrigatorio = p.Obrigatorio, descricao = p.Descricao })
        });
        var operadores = CatalogoAutomacao.Operadores.Select(o => new
        {
            valor = o.Valor.ToString(), nome = o.Nome, implementado = o.Implementado, precisaValor = o.PrecisaValor
        });
        return Ok(new { entidades, gatilhos, acoes, operadores });
    }
}

namespace AST.Comercial.Application.Dtos;

/// <summary>Resposta de POST odata/Propostas({id})/Compartilhar.</summary>
public record CompartilhamentoPropostaDto(string Chave, string Url);

/// <summary>Visão pública (anônima) de uma proposta compartilhada. Nunca inclui ids internos.</summary>
public class PropostaPublicaDto
{
    public int? Numero { get; set; }
    public string? Titulo { get; set; }
    public string? EmpresaNome { get; set; }
    public string? ClienteNome { get; set; }
    public decimal? Valor { get; set; }
    public string? Moeda { get; set; }
    public DateTime? ValidadeAte { get; set; }
    public string Status { get; set; } = "Aguardando";
    public List<ItemPropostaPublicoDto> Itens { get; set; } = [];
    public List<ParcelaPropostaPublicaDto> Parcelas { get; set; } = [];
    public string? DocumentoHtml { get; set; }
    public bool PodeResponder { get; set; }
}

public class ItemPropostaPublicoDto
{
    public string? Descricao { get; set; }
    public double? Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; }
    public double? Desconto { get; set; }
    public decimal? Total { get; set; }
}

public class ParcelaPropostaPublicaDto
{
    public int Numero { get; set; }
    public DateTime? Vencimento { get; set; }
    public decimal? Valor { get; set; }
}

public class AceitePropostaEntradaDto
{
    public string? Nome { get; set; }
    public string? Email { get; set; }
}

public class RecusaPropostaEntradaDto
{
    public string? Motivo { get; set; }
}

public enum ResultadoRespostaPublica
{
    Ok,
    NaoEncontrada,
    JaRespondida,
    NaoAceitaResposta,
    Invalida
}

public record RespostaPublicaResultado(ResultadoRespostaPublica Resultado, string? Status = null, string? Mensagem = null);

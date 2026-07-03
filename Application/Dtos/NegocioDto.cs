namespace AST.Comercial.Application.Dtos;

public record NegocioDto
{
    public long Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public decimal? Valor { get; init; }
    public long? StatusId { get; init; }
    public DateTime? FechadoEm { get; init; }
    public DateTime? PrevisaoFechamento { get; init; }
    public long ClienteId { get; init; }
    public long FunilId { get; init; }
    public long EtapaId { get; init; }
    public DateTime CriadoEm { get; init; }
}






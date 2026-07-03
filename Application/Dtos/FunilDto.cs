namespace AST.Comercial.Application.Dtos;

public record FunilDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public int Ordem { get; init; }
    public DateTime CriadoEm { get; init; }
}

public record EtapaFunilDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public int Ordem { get; init; }
    public int? DiasParaExpirar { get; init; }
    public long FunilId { get; init; }
    public DateTime CriadoEm { get; init; }
}





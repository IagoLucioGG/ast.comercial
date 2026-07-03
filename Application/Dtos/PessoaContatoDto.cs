namespace AST.Comercial.Application.Dtos;

public record PessoaContatoDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public string? Cargo { get; init; }
    public string? Documento { get; init; }
    public string? Observacoes { get; init; }
    public bool Decisor { get; init; }
    public long ClienteId { get; init; }
    public DateTime CriadoEm { get; init; }
}

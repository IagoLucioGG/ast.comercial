namespace AST.Comercial.Application.Dtos;

public record ClienteDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? RazaoSocial { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public string? Documento { get; init; }
    public string? Cidade { get; init; }
    public string? Estado { get; init; }
    public string? Endereco { get; init; }
    public string? Cep { get; init; }
    public string? Observacoes { get; init; }
    public string? Site { get; init; }
    public long? StatusClienteId { get; init; }
    public DateTime CriadoEm { get; init; }
}

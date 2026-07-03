using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Dtos;

public record AtividadeDto
{
    public long Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public TipoAtividade Tipo { get; init; }
    public DateTime? DataVencimento { get; init; }
    public DateTime? ConcluidaEm { get; init; }
    public bool Concluida { get; init; }
    public long? ClienteId { get; init; }
    public long? PessoaContatoId { get; init; }
    public long? NegocioId { get; init; }
    public DateTime CriadoEm { get; init; }
}





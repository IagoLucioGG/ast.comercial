namespace AST.Comercial.Application.Dtos;

/// <summary>
/// Corpo de criação de usuário. A senha chega em texto só aqui, é hasheada (BCrypt) e nunca devolvida.
/// Perfil/Tipo aceitam o nome do enum ("Gerente") ou o número.
/// </summary>
public class NovoUsuarioRequest
{
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
    public string? Perfil { get; set; }
    public string? Tipo { get; set; }
    public long? CargoId { get; set; }
    public long? DepartamentoId { get; set; }
    public string? FotoUrl { get; set; }
    public string? DescricaoIntegracao { get; set; }
}

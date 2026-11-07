namespace AST.Comercial.Application;

/// <summary>
/// Usuário sem alçada para a operação (ex.: aprovar fora do seu nível). Os controllers de aprovação a convertem
/// em HTTP 403 com a mensagem legível, no formato { erro, status }.
/// </summary>
public class AcessoNegadoException(string mensagem) : Exception(mensagem);

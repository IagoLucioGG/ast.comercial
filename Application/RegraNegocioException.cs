namespace AST.Comercial.Application;

/// <summary>
/// Violação de regra de negócio (ex.: mover negócio contra as regras do funil).
/// Mapeada para HTTP 422 pelo TratamentoErrosMiddleware. A mensagem é exibida ao usuário.
/// </summary>
public class RegraNegocioException(string mensagem) : Exception(mensagem);

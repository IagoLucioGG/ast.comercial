using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AST.Comercial.Infrastructure.Middleware;

/// <summary>
/// Corpo OData que não pôde ser lido (propriedade desconhecida, data-hora sem fuso, tipo errado...) chega ao controller
/// com <c>Delta&lt;T&gt;</c> nulo e ModelState inválido, o que antes virava NullReferenceException (HTTP 500).
/// Aqui vira 400 no mesmo formato do <see cref="TratamentoErrosMiddleware"/>: <c>{ erro, status }</c>.
/// Só atua em ODataController e só quando há um argumento Delta&lt;&gt;; os demais endpoints não são afetados.
/// </summary>
public class CorpoODataInvalidoFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is not ODataController) return;

        var temDelta = context.ActionDescriptor.Parameters
            .Any(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Delta<>));
        if (!temDelta) return;

        var deltaNulo = context.ActionArguments.Any(a =>
            a.Value is null && context.ActionDescriptor.Parameters.Any(p =>
                p.Name == a.Key && p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Delta<>)));

        if (context.ModelState.IsValid && !deltaNulo) return;

        var detalhe = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        var mensagem = "Corpo da requisição inválido.";
        if (!string.IsNullOrWhiteSpace(detalhe))
            mensagem += " " + (detalhe.Length > 300 ? detalhe[..300] : detalhe);

        context.Result = new JsonResult(new { erro = mensagem, status = 400 }) { StatusCode = 400 };
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

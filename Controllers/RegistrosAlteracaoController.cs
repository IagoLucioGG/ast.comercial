using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

/// <summary>
/// Auditoria (somente leitura). RegistroAlteracao não é EntidadeBase, então não há filtro global de tenant:
/// o isolamento é feito aqui, por EmpresaId. Linhas antigas sem empresa (EmpresaId nulo) não são exibidas.
/// </summary>
public class RegistrosAlteracaoController(AppDbContext db) : ODataController
{
    private IQueryable<RegistroAlteracao> DaEmpresa()
        => db.RegistrosAlteracao.AsNoTracking().Where(r => r.EmpresaId == db.EmpresaIdAtual && db.EmpresaIdAtual > 0);

    [EnableQuery(MaxTop = 100, PageSize = 100)]
    public IQueryable<RegistroAlteracao> Get() => DaEmpresa();

    [EnableQuery]
    public async Task<ActionResult<RegistroAlteracao>> Get(long key, CancellationToken ct)
    {
        var resultado = await DaEmpresa().FirstOrDefaultAsync(r => r.Id == key, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}

using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Controllers;

public record PermissaoItemDto(
    string Perfil, string Entidade, bool PodeVer, bool PodeCriar, bool PodeEditar, bool PodeExcluir, string Escopo);

public record SalvarPermissoesRequest(List<PermissaoItemDto>? Permissoes);

/// <summary>
/// Matriz de permissões por perfil × entidade. Catálogo e "minhas" servem a qualquer usuário autenticado
/// (o front precisa para montar menus); a matriz inteira e o PUT são só do Administrador.
/// </summary>
[ApiController]
[Route("api/permissoes")]
public class PermissoesController(AppDbContext db, IPermissaoServico servico) : ControllerBase
{
    /// <summary>Entidades cujo escopo de visibilidade é aplicado pelo servidor (filtro global do EF).</summary>
    private static readonly HashSet<string> EntidadesComEscopoAplicado =
        [CatalogoEntidades.Negocio, CatalogoEntidades.Atividade];

    private ObjectResult Erro(int status, string mensagem) => StatusCode(status, new { erro = mensagem, status });

    private bool EhAdministrador => db.PerfilAtual == PerfilUsuario.Administrador;

    [HttpGet("catalogo")]
    public ActionResult Catalogo() => Ok(CatalogoEntidades.Todas.Select(e => new
    {
        chave = e.Chave,
        rotulo = e.Rotulo,
        grupo = e.Grupo,
        suportaEscopo = EntidadesComEscopoAplicado.Contains(e.Chave)
    }));

    [HttpGet]
    public async Task<ActionResult> Matriz(CancellationToken ct)
    {
        if (!EhAdministrador) return Erro(403, "Somente o Administrador pode ver a matriz de permissões.");

        var linhas = await db.PermissoesPerfil.AsNoTracking().ToListAsync(ct);
        var porChave = linhas.ToDictionary(p => (p.Perfil, p.Entidade));

        // Completa com "sem acesso" o que ainda não existe no banco, para a tela sempre receber a grade inteira.
        var itens = new List<PermissaoItemDto>();
        foreach (var perfil in Enum.GetValues<PerfilUsuario>())
            foreach (var entidade in CatalogoEntidades.Todas)
            {
                if (perfil == PerfilUsuario.Administrador)
                    itens.Add(new(perfil.ToString(), entidade.Chave, true, true, true, true, nameof(EscopoVisibilidade.Todos)));
                else if (porChave.TryGetValue((perfil, entidade.Chave), out var p))
                    itens.Add(new(perfil.ToString(), entidade.Chave, p.PodeVer, p.PodeCriar, p.PodeEditar, p.PodeExcluir, p.Escopo.ToString()));
                else
                    itens.Add(new(perfil.ToString(), entidade.Chave, false, false, false, false, nameof(EscopoVisibilidade.Todos)));
            }

        return Ok(new { permissoes = itens });
    }

    [HttpPut]
    public async Task<ActionResult> Salvar([FromBody] SalvarPermissoesRequest? corpo, CancellationToken ct)
    {
        if (!EhAdministrador) return Erro(403, "Somente o Administrador pode alterar permissões.");
        if (corpo?.Permissoes is null || corpo.Permissoes.Count == 0)
            return Erro(400, "Informe a lista de permissões.");

        var recebidos = new Dictionary<(PerfilUsuario, string), PermissaoItemDto>();
        foreach (var item in corpo.Permissoes)
        {
            if (!Enum.TryParse<PerfilUsuario>(item.Perfil, ignoreCase: true, out var perfil) || !Enum.IsDefined(perfil))
                return Erro(400, $"Perfil inválido: {item.Perfil}.");
            if (!CatalogoEntidades.Existe(item.Entidade))
                return Erro(400, $"Entidade inválida: {item.Entidade}.");
            if (!Enum.TryParse<EscopoVisibilidade>(item.Escopo, ignoreCase: true, out var escopo) || !Enum.IsDefined(escopo))
                return Erro(400, $"Escopo inválido: {item.Escopo}.");

            if (perfil == PerfilUsuario.Administrador)
            {
                var completo = item.PodeVer && item.PodeCriar && item.PodeEditar && item.PodeExcluir && escopo == EscopoVisibilidade.Todos;
                if (!completo)
                    return Erro(422, "O perfil Administrador sempre tem todas as permissões e não pode ser restringido.");
            }

            recebidos[(perfil, item.Entidade)] = item; // última ocorrência vence
        }

        var existentes = await db.PermissoesPerfil
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == db.EmpresaIdAtual)
            .ToListAsync(ct);
        var porChave = existentes.ToDictionary(p => (p.Perfil, p.Entidade));

        foreach (var ((perfil, entidade), item) in recebidos)
        {
            Enum.TryParse<EscopoVisibilidade>(item.Escopo, true, out var escopo);

            // Criar/Editar/Excluir sem Ver não faz sentido: Ver passa a ser implícito.
            var ver = item.PodeVer || item.PodeCriar || item.PodeEditar || item.PodeExcluir;

            if (!porChave.TryGetValue((perfil, entidade), out var linha))
            {
                linha = new PermissaoPerfil { EmpresaId = db.EmpresaIdAtual, Perfil = perfil, Entidade = entidade };
                db.PermissoesPerfil.Add(linha);
            }

            linha.Ativo = true;
            linha.PodeVer = ver;
            linha.PodeCriar = item.PodeCriar;
            linha.PodeEditar = item.PodeEditar;
            linha.PodeExcluir = item.PodeExcluir;
            linha.Escopo = escopo;
        }

        await db.SaveChangesAsync(ct);
        servico.InvalidarEmpresa(db.EmpresaIdAtual);

        return await Matriz(ct);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult> Minhas(CancellationToken ct)
    {
        if (db.PerfilAtual is not { } perfil) return Erro(401, "Usuário não identificado.");

        IReadOnlyDictionary<string, PermissaoEfetiva> matriz = perfil == PerfilUsuario.Administrador
            ? new Dictionary<string, PermissaoEfetiva>()
            : await servico.ObterMatrizAsync(db.EmpresaIdAtual, perfil, ct);

        var itens = CatalogoEntidades.Todas.Select(e =>
        {
            var p = perfil == PerfilUsuario.Administrador
                ? PermissaoEfetiva.Total
                : matriz.GetValueOrDefault(e.Chave, PermissaoEfetiva.Nenhuma);
            // Criar/Editar/Excluir só valem com Ver (mesma regra do servidor).
            return new
            {
                entidade = e.Chave,
                podeVer = p.Ver,
                podeCriar = p.Ver && p.Criar,
                podeEditar = p.Ver && p.Editar,
                podeExcluir = p.Ver && p.Excluir,
                escopo = p.Escopo.ToString()
            };
        });

        return Ok(new
        {
            usuarioId = db.UsuarioIdAtual,
            perfil = perfil.ToString(),
            administrador = perfil == PerfilUsuario.Administrador,
            permissoes = itens
        });
    }
}

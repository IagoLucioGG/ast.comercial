using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Application.Interfaces;

/// <summary>Permissão efetiva de um perfil sobre uma entidade do catálogo.</summary>
public sealed record PermissaoEfetiva(bool Ver, bool Criar, bool Editar, bool Excluir, EscopoVisibilidade Escopo)
{
    public static readonly PermissaoEfetiva Nenhuma = new(false, false, false, false, EscopoVisibilidade.Todos);
    public static readonly PermissaoEfetiva Total = new(true, true, true, true, EscopoVisibilidade.Todos);
}

/// <summary>Dados do usuário autenticado que a autorização precisa (lidos do banco, não da claim do JWT).</summary>
/// <param name="CompanheirosDeEquipe">Ids dos usuários que compartilham alguma equipe com o usuário (inclui ele mesmo quando está em equipe).</param>
public sealed record ContextoUsuario(
    long Id, long EmpresaId, string Nome, PerfilUsuario Perfil, TipoUsuario Tipo, long[] CompanheirosDeEquipe);

public interface IPermissaoServico
{
    /// <summary>Matriz (entidade → permissão) do perfil na empresa. Em cache; chave ausente = sem permissão.</summary>
    Task<IReadOnlyDictionary<string, PermissaoEfetiva>> ObterMatrizAsync(long empresaId, PerfilUsuario perfil, CancellationToken ct = default);

    /// <summary>Usuário ativo da empresa (perfil, tipo e colegas de equipe). Nulo se não existe ou está inativo. Em cache curto.</summary>
    Task<ContextoUsuario?> ObterContextoUsuarioAsync(long empresaId, long usuarioId, CancellationToken ct = default);

    /// <summary>Descarta o cache da matriz da empresa (chamado ao salvar a matriz).</summary>
    void InvalidarEmpresa(long empresaId);
}

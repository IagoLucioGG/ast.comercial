namespace AST.Comercial.Domain.Entidades;

/// <summary>
/// Log de modificação - registra toda alteração feita em qualquer entidade.
/// Armazena o snapshot antes e depois da alteração (igual Ploomes).
/// </summary>
public class RegistroAlteracao
{
    public long Id { get; set; }

    /// <summary>Empresa (tenant) dona do registro. Nulo em linhas antigas que não puderam ser atribuídas com segurança.</summary>
    public long? EmpresaId { get; set; }
    /// <summary>Id do usuário que fez a alteração (nulo quando foi o sistema/automação/link público).</summary>
    public long? UsuarioId { get; set; }
    /// <summary>Nome do usuário no momento da alteração.</summary>
    public string? UsuarioNome { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public EntidadeAlvo EntidadeAlvo { get; set; }
    public long RegistroId { get; set; }
    public string? Titulo { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON com o estado completo do registro ANTES da alteração
    /// </summary>
    public string? DadosAntes { get; set; }

    /// <summary>
    /// JSON com o estado completo do registro DEPOIS da alteração
    /// </summary>
    public string? DadosDepois { get; set; }
}





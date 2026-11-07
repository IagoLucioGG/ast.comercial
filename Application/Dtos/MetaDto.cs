namespace AST.Comercial.Application.Dtos;

/// <summary>Ponto da série diária de uma meta. Acumulado é nulo nos dias futuros.</summary>
public record PontoMetaDto(DateTime Data, decimal? Acumulado, decimal AlvoLinear);

/// <summary>Posição de um usuário no ranking de uma meta de equipe/empresa.</summary>
public record RankingMetaDto(long UsuarioId, string Nome, decimal Realizado);

/// <summary>Progresso calculado de uma meta a partir dos dados reais.</summary>
public record ProgressoMetaDto(
    long Id,
    string Nome,
    string Metrica,
    string Escopo, // usuario | equipe | empresa
    decimal Alvo,
    decimal Realizado,
    decimal Percentual,
    decimal? Projecao,
    decimal? PercentualProjetado,
    bool NoRitmo,
    long? MoedaId,
    string? MoedaSimbolo,
    string? MoedaCodigo,
    long? UsuarioId,
    string? UsuarioNome,
    long? EquipeId,
    string? EquipeNome,
    long? FunilId,
    string? FunilNome,
    DateTime Inicio,
    DateTime Fim,
    int DiasTotais,
    int DiasDecorridos,
    IReadOnlyList<PontoMetaDto> Serie,
    IReadOnlyList<RankingMetaDto> Ranking);

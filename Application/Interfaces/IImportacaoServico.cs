using AST.Comercial.Application.Importacoes;

namespace AST.Comercial.Application.Interfaces;

public sealed record CampoImportavelDto(
    string Chave, string Rotulo, string Tipo, bool Obrigatorio, bool Personalizado, int Tamanho, string? Dica);

public sealed record SugestaoMapeamentoDto(int Coluna, string Cabecalho, string Campo, double Pontuacao);

public sealed record AnaliseImportacaoDto(
    string Entidade, string EntidadeRotulo, string NomeArquivo, string Codificacao, string Separador, int TotalLinhas,
    string[] Cabecalhos, string[][] Amostra, List<CampoImportavelDto> Campos, List<SugestaoMapeamentoDto> Sugestao,
    List<ChaveDuplicidadeDto> ChavesDuplicidade);

public sealed record ChaveDuplicidadeDto(string Chave, string Rotulo);

public sealed record ValidacaoImportacaoDto(
    int TotalLinhas, int Validas, int Invalidas, int Duplicadas, int TotalErros, bool ErrosTruncados, List<ErroLinha> Erros);

public sealed record ImportacaoDto(
    long Id, string Entidade, string EntidadeRotulo, string NomeArquivo, string Status, int TotalLinhas, int Sucesso, int Falhas,
    int Criadas, int Atualizadas, int Ignoradas, int Processadas, string Duplicidade, string? Chave, bool TemErros,
    string? UsuarioNome, DateTime CriadoEm, DateTime? ConcluidaEm);

public interface IImportacaoServico
{
    Task<AnaliseImportacaoDto> AnalisarAsync(string entidade, string nomeArquivo, byte[] bytes, CancellationToken ct = default);

    Task<ValidacaoImportacaoDto> ValidarAsync(
        string entidade, byte[] bytes, string? mapeamentoJson, string? duplicidade, string? chave, CancellationToken ct = default);

    /// <summary>Valida, cria o registro da importação e inicia a gravação em segundo plano.</summary>
    Task<ImportacaoDto> ExecutarAsync(
        string entidade, string nomeArquivo, byte[] bytes, string? mapeamentoJson, string? duplicidade, string? chave,
        CancellationToken ct = default);

    Task<(List<ImportacaoDto> Itens, int Total)> ListarAsync(int pagina, int tamanho, CancellationToken ct = default);
    Task<ImportacaoDto?> ObterAsync(long id, CancellationToken ct = default);

    /// <summary>CSV de erros (UTF-8 com BOM, separador ;) com proteção contra injeção de fórmula; nulo se a importação não existe.</summary>
    Task<string?> GerarRelatorioErrosAsync(long id, CancellationToken ct = default);

    /// <summary>Modelo CSV (cabeçalhos dos campos nativos e personalizados da empresa).</summary>
    Task<string> GerarModeloAsync(string entidade, CancellationToken ct = default);
}

using System.Text;

namespace AST.Comercial.Application.Importacoes;

/// <summary>Arquivo recusado (tipo, tamanho, limite de linhas ou conteúdo ilegível). Vira HTTP 422/413 no controller.</summary>
public class ArquivoImportacaoException(string mensagem, int status = 422) : Exception(mensagem)
{
    public int Status { get; } = status;
}

/// <param name="Codificacao">Nome amigável da codificação detectada.</param>
/// <param name="Separador">Separador detectado (; , ou tab).</param>
/// <param name="Registros">Registros do arquivo; o primeiro é o cabeçalho. Registros totalmente vazios são descartados.</param>
public sealed record CsvLido(string Codificacao, char Separador, List<string[]> Registros);

/// <summary>
/// Leitor de CSV em memória (RFC 4180 + tolerâncias de planilha): UTF-8 com/sem BOM, UTF-16 com BOM e Windows-1252,
/// separador ; , ou tab detectado, campos entre aspas (com "" e quebras de linha dentro). Nada é gravado em disco.
/// </summary>
public static class CsvLeitor
{
    public const int LimiteBytes = 5 * 1024 * 1024;
    public const int LimiteLinhas = 10_000;

    private const string MensagemLimiteLinhas = "O arquivo tem mais de 10.000 linhas de dados.";

    private static readonly char[] Candidatos = [';', ',', '\t'];

    static CsvLeitor()
    {
        // Windows-1252 não vem por padrão no .NET moderno; o provedor está no framework compartilhado.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static CsvLido Ler(byte[] bytes)
    {
        if (bytes.Length == 0) throw new ArquivoImportacaoException("O arquivo está vazio.");
        if (bytes.Length > LimiteBytes)
            throw new ArquivoImportacaoException("O arquivo excede o limite de 5 MB.", 413);

        VerificarAssinaturaBinaria(bytes);
        var (texto, codificacao) = Decodificar(bytes);
        if (texto.Contains('\0'))
            throw new ArquivoImportacaoException("O arquivo não é um CSV de texto válido.");

        var separador = DetectarSeparador(texto);
        var registros = Analisar(texto, separador);
        if (registros.Count == 0) throw new ArquivoImportacaoException("O arquivo não tem nenhuma linha.");
        if (registros.Count < 2) throw new ArquivoImportacaoException("O arquivo tem só o cabeçalho; não há linhas de dados.");
        return new CsvLido(codificacao, separador, registros);
    }

    private static void VerificarAssinaturaBinaria(byte[] b)
    {
        static bool Comeca(byte[] dados, params byte[] prefixo)
            => dados.Length >= prefixo.Length && dados.AsSpan(0, prefixo.Length).SequenceEqual(prefixo);

        if (Comeca(b, 0x50, 0x4B, 0x03, 0x04)                       // zip (xlsx, docx...)
            || Comeca(b, 0xD0, 0xCF, 0x11, 0xE0)                    // OLE (xls antigo)
            || Comeca(b, 0x25, 0x50, 0x44, 0x46)                    // %PDF
            || Comeca(b, 0x4D, 0x5A)                                // MZ (executável)
            || Comeca(b, 0x89, 0x50, 0x4E, 0x47)                    // PNG
            || Comeca(b, 0xFF, 0xD8, 0xFF)                          // JPEG
            || Comeca(b, 0x47, 0x49, 0x46, 0x38)                    // GIF
            || Comeca(b, 0x7F, 0x45, 0x4C, 0x46))                   // ELF
            throw new ArquivoImportacaoException("Formato não aceito. Envie um arquivo CSV (texto separado por ; ou ,).");
    }

    private static (string Texto, string Codificacao) Decodificar(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            return (new UTF8Encoding(false).GetString(b, 3, b.Length - 3), "UTF-8 (com BOM)");
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            return (Encoding.Unicode.GetString(b, 2, b.Length - 2), "UTF-16 LE");
        if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            return (Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2), "UTF-16 BE");

        try
        {
            return (new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(b), "UTF-8");
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.GetEncoding(1252).GetString(b), "Windows-1252");
        }
    }

    /// <summary>Escolhe o separador que dá colunas consistentes (e > 1) nos primeiros registros.</summary>
    private static char DetectarSeparador(string texto)
    {
        var amostra = texto.Length > 16384 ? texto[..16384] : texto;
        var truncada = texto.Length > 16384;
        var melhor = ';';
        var melhorPontos = -1;
        foreach (var candidato in Candidatos)
        {
            List<string[]> regs;
            try { regs = Analisar(amostra, candidato, limite: 12, tolerante: true); }
            catch (ArquivoImportacaoException) { continue; }
            if (truncada && regs.Count > 1) regs.RemoveAt(regs.Count - 1); // último pode estar cortado
            if (regs.Count == 0) continue;
            var colunas = regs[0].Length;
            if (colunas < 2) continue;
            var pontos = colunas + regs.Count(r => r.Length == colunas) * 1000;
            if (pontos > melhorPontos) { melhorPontos = pontos; melhor = candidato; }
        }
        return melhor;
    }

    private static List<string[]> Analisar(string texto, char sep, int limite = int.MaxValue, bool tolerante = false)
    {
        var registros = new List<string[]>();
        var campos = new List<string>();
        var atual = new StringBuilder();
        var entreAspas = false;
        var campoComAspas = false;

        void FecharCampo()
        {
            campos.Add(atual.ToString());
            atual.Clear();
            campoComAspas = false;
        }

        void FecharRegistro()
        {
            FecharCampo();
            var vazio = campos.Count == 1 && string.IsNullOrWhiteSpace(campos[0]);
            if (!vazio) registros.Add(campos.ToArray());
            campos.Clear();
        }

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];
            if (entreAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"') { atual.Append('"'); i++; }
                    else entreAspas = false;
                }
                else atual.Append(c);
                continue;
            }

            if (c == '"' && atual.Length == 0 && !campoComAspas) { entreAspas = true; campoComAspas = true; }
            else if (c == sep) FecharCampo();
            else if (c == '\r')
            {
                if (i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                FecharRegistro();
                if (registros.Count > LimiteLinhas + 1 && !tolerante)
                    throw new ArquivoImportacaoException(MensagemLimiteLinhas);
                if (registros.Count >= limite) return registros;
            }
            else if (c == '\n')
            {
                FecharRegistro();
                if (registros.Count > LimiteLinhas + 1 && !tolerante)
                    throw new ArquivoImportacaoException(MensagemLimiteLinhas);
                if (registros.Count >= limite) return registros;
            }
            else atual.Append(c);
        }

        if (entreAspas && !tolerante)
            throw new ArquivoImportacaoException("Há aspas sem fechar no arquivo. Verifique os campos entre aspas.");
        if (atual.Length > 0 || campos.Count > 0 || campoComAspas) FecharRegistro();

        if (!tolerante && registros.Count > LimiteLinhas + 1)
            throw new ArquivoImportacaoException(MensagemLimiteLinhas);
        return registros;
    }

    /// <summary>Escreve uma linha CSV (separador ;). Aplica a proteção contra injeção de fórmula em todas as células.</summary>
    public static string Linha(IEnumerable<string?> celulas, bool protegerFormula = true)
        => string.Join(';', celulas.Select(c => Escapar(c, protegerFormula)));

    public static string Escapar(string? valor, bool protegerFormula = true)
    {
        var v = valor ?? string.Empty;
        // Planilhas executam células que começam com = + - @ (e tab/CR, usados para esconder o prefixo). Prefixa com apóstrofo.
        if (protegerFormula && v.Length > 0 && (v[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
            v = "'" + v;
        if (v.IndexOfAny([';', '"', '\n', '\r', ',']) >= 0)
            v = "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}

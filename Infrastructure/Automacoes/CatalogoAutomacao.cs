using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using AST.Comercial.Domain.Entidades;

namespace AST.Comercial.Infrastructure.Automacoes;

public record DefGatilho(TipoGatilho Valor, string Nome, bool Implementado, EntidadeAlvo[] Entidades, string? Observacao);
public record DefParametro(string Chave, string Tipo, bool Obrigatorio, string Descricao);
public record DefAcao(TipoAcao Valor, string Nome, bool Implementado, EntidadeAlvo[]? Entidades, DefParametro[] Configuracao);
public record DefOperador(OperadorFiltro Valor, string Nome, bool Implementado, bool PrecisaValor);
public record DefCampo(string Nome, string Tipo, string[]? Opcoes, bool Editavel);

/// <summary>
/// Catálogo único do que o motor realmente executa. Usado pelo endpoint de catálogo e pelas validações
/// (fonte da verdade: se mudar o motor, mude aqui e em mapeamento/analise/07-contrato-automacoes.md).
/// </summary>
public static partial class CatalogoAutomacao
{
    public static readonly EntidadeAlvo[] EntidadesAlvo =
    [
        EntidadeAlvo.Cliente, EntidadeAlvo.PessoaContato, EntidadeAlvo.Negocio,
        EntidadeAlvo.Atividade, EntidadeAlvo.Proposta, EntidadeAlvo.Funil
    ];

    private static readonly EntidadeAlvo[] CrudBasico =
        [EntidadeAlvo.Cliente, EntidadeAlvo.PessoaContato, EntidadeAlvo.Negocio, EntidadeAlvo.Atividade, EntidadeAlvo.Funil];

    public static readonly Dictionary<EntidadeAlvo, string> NomesEntidade = new()
    {
        [EntidadeAlvo.Cliente] = "Cliente",
        [EntidadeAlvo.PessoaContato] = "Pessoa de contato",
        [EntidadeAlvo.Negocio] = "Negócio",
        [EntidadeAlvo.Atividade] = "Atividade",
        [EntidadeAlvo.Proposta] = "Proposta",
        [EntidadeAlvo.Funil] = "Funil"
    };

    private static readonly Dictionary<EntidadeAlvo, Type> TiposEntidade = new()
    {
        [EntidadeAlvo.Cliente] = typeof(Cliente),
        [EntidadeAlvo.PessoaContato] = typeof(PessoaContato),
        [EntidadeAlvo.Negocio] = typeof(Negocio),
        [EntidadeAlvo.Atividade] = typeof(Atividade),
        [EntidadeAlvo.Proposta] = typeof(Proposta),
        [EntidadeAlvo.Funil] = typeof(Funil)
    };

    /// <summary>Entidades onde EditarDados é permitido (as que o motor sabe carregar).</summary>
    private static readonly EntidadeAlvo[] EntidadesEditaveis =
        [EntidadeAlvo.Cliente, EntidadeAlvo.PessoaContato, EntidadeAlvo.Negocio, EntidadeAlvo.Atividade, EntidadeAlvo.Proposta];

    public static readonly DefGatilho[] Gatilhos =
    [
        new(TipoGatilho.Criado, "Criado", true, CrudBasico, null),
        new(TipoGatilho.Editado, "Editado", true, CrudBasico, null),
        new(TipoGatilho.Removido, "Removido", true, CrudBasico, null),
        new(TipoGatilho.Concluido, "Concluído", true, [EntidadeAlvo.Atividade], null),
        new(TipoGatilho.StatusMudou, "Status mudou", true, [EntidadeAlvo.Negocio, EntidadeAlvo.Proposta], "Proposta: disparado ao rejeitar."),
        new(TipoGatilho.EtapaMudou, "Etapa mudou", true, [EntidadeAlvo.Negocio], null),
        new(TipoGatilho.PropostaCriada, "Proposta criada", true, [EntidadeAlvo.Proposta], null),
        new(TipoGatilho.PropostaAprovada, "Proposta aprovada", true, [EntidadeAlvo.Proposta], "Aprovação interna ou aceite pelo link público."),
        new(TipoGatilho.PropostaEnviada, "Proposta enviada", true, [EntidadeAlvo.Proposta], "Disparado ao compartilhar a proposta pela primeira vez."),
        new(TipoGatilho.Periodicamente, "Periodicamente", true, EntidadesAlvo,
            "Executa uma vez por dia no horário informado (UTC), sem registro associado; só aceita as ações CriarTarefa, CriarRegistroInteracao e ExecutarWebhook."),
        new(TipoGatilho.CampoAlterado, "Campo alterado", false, [], "Ainda não há emissor do evento."),
        new(TipoGatilho.Vencido, "Vencido", false, [], "Ainda não há emissor do evento."),
        new(TipoGatilho.PropostaExpirada, "Proposta expirada", false, [], "Ainda não há emissor do evento."),
        new(TipoGatilho.EtiquetaAdicionada, "Etiqueta adicionada", false, [], "Ainda não há emissor do evento.")
    ];

    public static readonly TipoAcao[] AcoesSemRegistro =
        [TipoAcao.CriarTarefa, TipoAcao.CriarRegistroInteracao, TipoAcao.ExecutarWebhook];

    public static readonly DefAcao[] Acoes =
    [
        new(TipoAcao.EditarDados, "Editar dados", true, EntidadesEditaveis,
            [new("alteracoes", "lista", true, "Lista de { campo, valor }. Campo escalar gravável da entidade.")]),
        new(TipoAcao.CriarTarefa, "Criar tarefa", true, null,
        [
            new("titulo", "texto", true, "Título da tarefa (até 200)."),
            new("tipo", "texto", false, "Tarefa, Ligacao, Email, Reuniao ou Nota."),
            new("prazo", "numero", false, "Prazo em dias a partir da execução (0 a 3650; padrão 3).")
        ]),
        new(TipoAcao.AlterarStatus, "Alterar status", true, [EntidadeAlvo.Negocio],
            [new("statusId", "numero", true, "Id do status do negócio.")]),
        new(TipoAcao.MoverEtapa, "Mover etapa", true, [EntidadeAlvo.Negocio],
            [new("etapaId", "numero", true, "Id da etapa de destino (o funil acompanha a etapa).")]),
        new(TipoAcao.ConcluirTarefa, "Concluir tarefa", true, [EntidadeAlvo.Atividade], []),
        new(TipoAcao.AdicionarEtiqueta, "Adicionar etiqueta", true, null,
            [new("etiquetaId", "numero", true, "Id da etiqueta.")]),
        new(TipoAcao.RemoverEtiqueta, "Remover etiqueta", true, null,
            [new("etiquetaId", "numero", true, "Id da etiqueta.")]),
        new(TipoAcao.CriarRegistroInteracao, "Registrar no histórico", true, null,
            [new("descricao", "texto", true, "Texto do registro (até 1000).")]),
        new(TipoAcao.ExecutarWebhook, "Chamar webhook", true, null,
        [
            new("url", "texto", true, "URL http/https pública (hosts locais/privados são bloqueados)."),
            new("metodo", "texto", false, "POST (padrão), GET, PUT, PATCH ou DELETE."),
            new("headers", "objeto", false, "Cabeçalhos { nome: valor }."),
            new("corpo", "texto", false, "Corpo da requisição (string, enviado como JSON).")
        ]),
        new(TipoAcao.EnviarEmail, "Enviar e-mail", false, null, []),
        new(TipoAcao.NotificarUsuario, "Notificar usuário", false, null, []),
        new(TipoAcao.CriarContato, "Criar contato", false, null, []),
        new(TipoAcao.CriarProposta, "Criar proposta", false, null, []),
        new(TipoAcao.AlterarResponsavel, "Alterar responsável", false, null, []),
        new(TipoAcao.CriarNegocio, "Criar negócio", false, null, []),
        new(TipoAcao.GerarDocumento, "Gerar documento", false, null, [])
    ];

    public static readonly DefOperador[] Operadores =
    [
        new(OperadorFiltro.Igual, "Igual a", true, true),
        new(OperadorFiltro.Diferente, "Diferente de", true, true),
        new(OperadorFiltro.Contem, "Contém", true, true),
        new(OperadorFiltro.NaoContem, "Não contém", true, true),
        new(OperadorFiltro.Maior, "Maior que", true, true),
        new(OperadorFiltro.Menor, "Menor que", true, true),
        new(OperadorFiltro.MaiorOuIgual, "Maior ou igual a", true, true),
        new(OperadorFiltro.MenorOuIgual, "Menor ou igual a", true, true),
        new(OperadorFiltro.Vazio, "Está vazio", true, false),
        new(OperadorFiltro.NaoVazio, "Não está vazio", true, false),
        new(OperadorFiltro.ComecaCom, "Começa com", true, true),
        new(OperadorFiltro.TerminaCom, "Termina com", true, true),
        new(OperadorFiltro.MudouPara, "Mudou para", false, true),
        new(OperadorFiltro.MudouDe, "Mudou de", false, true)
    ];

    /// <summary>Propriedades que a automação nunca pode gravar via EditarDados.</summary>
    public static readonly HashSet<string> CamposProtegidos = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(EntidadeBase.Id), nameof(EntidadeBase.EmpresaId), nameof(EntidadeBase.Ativo),
        nameof(EntidadeBase.CriadoEm), nameof(EntidadeBase.AtualizadoEm), nameof(EntidadeBase.ChaveExterna),
        nameof(Proposta.Chave), nameof(Proposta.Compartilhada)
    };

    public static Type? TipoClr(EntidadeAlvo entidade) => TiposEntidade.GetValueOrDefault(entidade);

    public static IReadOnlyList<DefCampo> Campos(EntidadeAlvo entidade)
    {
        var tipo = TipoClr(entidade);
        if (tipo is null) return [];
        var lista = new List<DefCampo>();
        foreach (var p in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.MetadataToken))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            string? nomeTipo = null; string[]? opcoes = null;
            if (t == typeof(string)) nomeTipo = "texto";
            else if (t == typeof(bool)) nomeTipo = "booleano";
            else if (t == typeof(int) || t == typeof(long) || t == typeof(decimal) || t == typeof(double) || t == typeof(float)) nomeTipo = "numero";
            else if (t == typeof(DateTime)) nomeTipo = "data";
            else if (t.IsEnum) { nomeTipo = "enum"; opcoes = Enum.GetNames(t); }
            if (nomeTipo is null) continue;
            lista.Add(new DefCampo(p.Name, nomeTipo, opcoes, !CamposProtegidos.Contains(p.Name)));
        }
        return lista;
    }

    /// <summary>Retorna a propriedade escalar gravável (ou null).</summary>
    public static PropertyInfo? PropriedadeEditavel(Type tipo, string campo)
    {
        if (CamposProtegidos.Contains(campo)) return null;
        var p = tipo.GetProperty(campo, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (p is null || !p.CanWrite) return null;
        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        var escalar = t == typeof(string) || t == typeof(bool) || t == typeof(int) || t == typeof(long)
            || t == typeof(decimal) || t == typeof(double) || t == typeof(DateTime) || t.IsEnum;
        return escalar && !CamposProtegidos.Contains(p.Name) ? p : null;
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,99}$")]
    public static partial Regex RegexCampo();

    /// <summary>Valida a configuração de uma ação conforme o catálogo. Retorna mensagens de erro.</summary>
    public static List<string> ValidarConfiguracao(TipoAcao tipo, EntidadeAlvo entidade, JsonElement? config, string prefixo)
    {
        var erros = new List<string>();
        if (config is null || config.Value.ValueKind != JsonValueKind.Object)
        {
            if (tipo == TipoAcao.ConcluirTarefa && (config is null || config.Value.ValueKind == JsonValueKind.Null)) return erros;
            erros.Add($"{prefixo}: configuração deve ser um objeto JSON.");
            return erros;
        }
        var c = config.Value;

        switch (tipo)
        {
            case TipoAcao.EditarDados:
            {
                if (!c.TryGetProperty("alteracoes", out var alt) || alt.ValueKind != JsonValueKind.Array || alt.GetArrayLength() == 0)
                { erros.Add($"{prefixo}: informe ao menos uma alteração em 'alteracoes'."); break; }
                if (alt.GetArrayLength() > 50) { erros.Add($"{prefixo}: no máximo 50 alterações."); break; }
                var clr = TipoClr(entidade);
                foreach (var a in alt.EnumerateArray())
                {
                    if (a.ValueKind != JsonValueKind.Object || !a.TryGetProperty("campo", out var campo) || campo.ValueKind != JsonValueKind.String
                        || !a.TryGetProperty("valor", out var valor))
                    { erros.Add($"{prefixo}: cada alteração precisa de 'campo' (texto) e 'valor'."); continue; }
                    var nome = campo.GetString() ?? "";
                    if (!RegexCampo().IsMatch(nome) || clr is null || PropriedadeEditavel(clr, nome) is null)
                        erros.Add($"{prefixo}: o campo '{nome}' não existe ou não pode ser alterado em {entidade}.");
                    if (valor.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        erros.Add($"{prefixo}: o valor de '{nome}' deve ser texto, número, booleano ou nulo.");
                    else if (valor.ValueKind == JsonValueKind.String && (valor.GetString()?.Length ?? 0) > 2000)
                        erros.Add($"{prefixo}: o valor de '{nome}' excede 2000 caracteres.");
                }
                break;
            }
            case TipoAcao.CriarTarefa:
            {
                if (!c.TryGetProperty("titulo", out var t) || t.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(t.GetString()))
                    erros.Add($"{prefixo}: 'titulo' é obrigatório.");
                else if (t.GetString()!.Length > 200) erros.Add($"{prefixo}: 'titulo' excede 200 caracteres.");
                if (c.TryGetProperty("tipo", out var tp) && tp.ValueKind != JsonValueKind.Null)
                {
                    if (tp.ValueKind != JsonValueKind.String || !Enum.TryParse<TipoAtividade>(tp.GetString(), true, out var ta) || !Enum.IsDefined(ta))
                        erros.Add($"{prefixo}: 'tipo' inválido. Use {string.Join(", ", Enum.GetNames<TipoAtividade>())}.");
                }
                if (c.TryGetProperty("prazo", out var pz) && pz.ValueKind != JsonValueKind.Null
                    && !(pz.ValueKind == JsonValueKind.Number && pz.TryGetInt32(out var dias) && dias is >= 0 and <= 3650))
                    erros.Add($"{prefixo}: 'prazo' deve ser um inteiro de 0 a 3650 (dias).");
                break;
            }
            case TipoAcao.AlterarStatus:
                ExigirId(c, "statusId", prefixo, erros); break;
            case TipoAcao.MoverEtapa:
                ExigirId(c, "etapaId", prefixo, erros); break;
            case TipoAcao.AdicionarEtiqueta:
            case TipoAcao.RemoverEtiqueta:
                ExigirId(c, "etiquetaId", prefixo, erros); break;
            case TipoAcao.CriarRegistroInteracao:
                if (!c.TryGetProperty("descricao", out var d) || d.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(d.GetString()))
                    erros.Add($"{prefixo}: 'descricao' é obrigatória.");
                else if (d.GetString()!.Length > 1000) erros.Add($"{prefixo}: 'descricao' excede 1000 caracteres.");
                break;
            case TipoAcao.ExecutarWebhook:
            {
                if (!c.TryGetProperty("url", out var u) || u.ValueKind != JsonValueKind.String)
                { erros.Add($"{prefixo}: 'url' é obrigatória."); break; }
                var erroUrl = ValidarUrlWebhook(u.GetString());
                if (erroUrl is not null) erros.Add($"{prefixo}: {erroUrl}");
                if (c.TryGetProperty("metodo", out var m) && m.ValueKind != JsonValueKind.Null
                    && !(m.ValueKind == JsonValueKind.String && MetodosWebhook.Contains(m.GetString()!.ToUpperInvariant())))
                    erros.Add($"{prefixo}: 'metodo' deve ser {string.Join(", ", MetodosWebhook)}.");
                if (c.TryGetProperty("headers", out var h) && h.ValueKind != JsonValueKind.Null)
                {
                    if (h.ValueKind != JsonValueKind.Object) erros.Add($"{prefixo}: 'headers' deve ser um objeto.");
                    else foreach (var hp in h.EnumerateObject())
                        if (hp.Value.ValueKind != JsonValueKind.String || !HeaderValido().IsMatch(hp.Name))
                        { erros.Add($"{prefixo}: header '{hp.Name}' inválido."); break; }
                }
                if (c.TryGetProperty("corpo", out var co) && co.ValueKind != JsonValueKind.Null
                    && (co.ValueKind != JsonValueKind.String || co.GetString()!.Length > 20000))
                    erros.Add($"{prefixo}: 'corpo' deve ser texto de até 20000 caracteres.");
                break;
            }
        }
        return erros;
    }

    private static void ExigirId(JsonElement c, string chave, string prefixo, List<string> erros)
    {
        if (!c.TryGetProperty(chave, out var v) || v.ValueKind != JsonValueKind.Number || !v.TryGetInt64(out var id) || id <= 0)
            erros.Add($"{prefixo}: '{chave}' é obrigatório (número positivo).");
    }

    public static long LerId(JsonElement c, string chave) => c.GetProperty(chave).GetInt64();

    private static readonly string[] MetodosWebhook = ["POST", "GET", "PUT", "PATCH", "DELETE"];

    [GeneratedRegex(@"^[A-Za-z0-9\-]{1,100}$")]
    private static partial Regex HeaderValido();

    // ---------- Webhook / SSRF ----------

    /// <summary>Validação estática da URL (esquema e host literal). A resolução de DNS é checada na execução.</summary>
    public static string? ValidarUrlWebhook(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 2000) return "'url' inválida.";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return "'url' deve ser http ou https.";
        if (!string.IsNullOrEmpty(uri.UserInfo)) return "'url' não pode conter usuário/senha.";
        var host = uri.Host.ToLowerInvariant();
        if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local") || host.EndsWith(".internal"))
            return "'url' aponta para um host local/privado, que não é permitido.";
        if (IPAddress.TryParse(host.Trim('[', ']'), out var ip) && EhEnderecoPrivado(ip))
            return "'url' aponta para um endereço local/privado, que não é permitido.";
        return null;
    }

    public static bool EhEnderecoPrivado(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || (ip.GetAddressBytes()[0] & 0xFE) == 0xFC;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || b[0] == 127 || b[0] == 0
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 100 && b[1] is >= 64 and <= 127)
            || b[0] >= 224;
    }
}

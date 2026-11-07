using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Infrastructure.Autorizacao;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AST.Comercial.Tests.Autorizacao;

/// <summary>Regras de rota → permissão. Lógica pura, sem banco.</summary>
public class MapaPermissaoRotasTests
{
    private static RegraRota Resolver(string metodo, string caminho, Dictionary<string, StringValues>? query = null, long? usuarioId = null)
        => MapaPermissaoRotas.Resolver(metodo, new PathString(caminho), new QueryCollection(query ?? []), usuarioId);

    [Theory]
    [InlineData("GET", "/odata/Negocios", AcaoPermissao.Ver)]
    [InlineData("POST", "/odata/Negocios", AcaoPermissao.Criar)]
    [InlineData("PATCH", "/odata/Negocios(1)", AcaoPermissao.Editar)]
    [InlineData("DELETE", "/odata/Negocios(1)", AcaoPermissao.Excluir)]
    public void Verbo_define_a_acao_sobre_a_entidade(string metodo, string caminho, AcaoPermissao esperada)
    {
        var regra = Resolver(metodo, caminho);

        Assert.Equal(TipoRegra.Entidade, regra.Tipo);
        Assert.Equal(CatalogoEntidades.Negocio, regra.Entidade);
        Assert.Equal(esperada, regra.Acao);
    }

    [Fact]
    public void Post_de_acao_no_registro_conta_como_edicao()
    {
        var regra = Resolver("POST", "/odata/Negocios(1)/Ganhar");

        Assert.Equal(AcaoPermissao.Editar, regra.Acao);
        Assert.Equal(CatalogoEntidades.Negocio, regra.Entidade);
    }

    [Fact]
    public void Renderizar_modelo_e_somente_leitura_mesmo_via_post()
    {
        var regra = Resolver("POST", "/odata/ModelosDocumento(3)/Renderizar(10)");

        Assert.Equal(AcaoPermissao.Ver, regra.Acao);
    }

    [Theory]
    [InlineData("/odata/EntidadeInexistente")]
    [InlineData("/odata/$batch")]
    [InlineData("/api/rota-desconhecida")]
    public void Rotas_desconhecidas_falham_fechadas(string caminho)
    {
        Assert.Equal(TipoRegra.NaoMapeada, Resolver("GET", caminho).Tipo);
    }

    [Fact]
    public void Verbo_desconhecido_falha_fechado()
    {
        Assert.Equal(TipoRegra.NaoMapeada, Resolver("TRACE", "/odata/Clientes").Tipo);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/odata")]
    [InlineData("/odata/$metadata")]
    [InlineData("/api/autenticacao/login")]
    [InlineData("/api/publico/propostas/abc")]
    public void Rotas_abertas_nao_exigem_permissao(string caminho)
    {
        Assert.Equal(TipoRegra.Livre, Resolver("GET", caminho).Tipo);
    }

    [Fact]
    public void Matriz_de_permissoes_so_para_administrador()
    {
        Assert.Equal(TipoRegra.SomenteAdministrador, Resolver("GET", "/odata/PermissoesPerfil").Tipo);
    }

    [Fact]
    public void Importar_modelo_ploomes_so_para_administrador()
    {
        Assert.Equal(TipoRegra.SomenteAdministrador, Resolver("POST", "/api/importar-modelo-ploomes").Tipo);
    }

    [Fact]
    public void Dados_de_referencia_de_formulario_tem_leitura_livre_e_escrita_restrita()
    {
        Assert.Equal(TipoRegra.Livre, Resolver("GET", "/odata/Campos").Tipo);
        Assert.Equal(TipoRegra.Entidade, Resolver("POST", "/odata/Campos").Tipo);
    }

    [Fact]
    public void Cadastro_auxiliar_le_livre_e_escreve_com_a_acao_do_verbo()
    {
        Assert.Equal(TipoRegra.Livre, Resolver("GET", "/odata/Clientes@Origens").Tipo);

        var escrita = Resolver("POST", "/odata/Clientes@Origens");
        Assert.Equal(CatalogoEntidades.CadastroAuxiliar, escrita.Entidade);
        Assert.Equal(AcaoPermissao.Criar, escrita.Acao);
    }

    [Fact]
    public void Registro_filho_exige_editar_no_pai_mesmo_ao_criar_ou_excluir()
    {
        Assert.Equal(AcaoPermissao.Editar, Resolver("POST", "/odata/ItensProposta").Acao);
        Assert.Equal(AcaoPermissao.Editar, Resolver("DELETE", "/odata/ItensProposta(5)").Acao);
        Assert.Equal(CatalogoEntidades.Proposta, Resolver("POST", "/odata/ItensProposta").Entidade);
    }

    [Fact]
    public void Historico_de_alteracoes_so_le_e_exige_ver_em_alguma_entidade_comercial()
    {
        Assert.Equal(TipoRegra.VerComercialQualquer, Resolver("GET", "/odata/RegistrosAlteracao").Tipo);
        Assert.Equal(TipoRegra.NaoMapeada, Resolver("DELETE", "/odata/RegistrosAlteracao(1)").Tipo);
    }

    [Fact]
    public void Lista_de_selecao_de_usuarios_e_aberta_somente_com_campos_publicos()
    {
        var publico = new Dictionary<string, StringValues> { ["$select"] = "Id,Nome,Ativo,FotoUrl" };
        Assert.Equal(TipoRegra.Livre, Resolver("GET", "/odata/Usuarios", publico).Tipo);

        var comEmail = new Dictionary<string, StringValues> { ["$select"] = "Id,Nome,Email" };
        Assert.Equal(TipoRegra.Entidade, Resolver("GET", "/odata/Usuarios", comEmail).Tipo);

        Assert.Equal(TipoRegra.Entidade, Resolver("GET", "/odata/Usuarios").Tipo);
    }

    [Fact]
    public void Lista_de_usuarios_nao_vaza_dado_sensivel_por_filtro_ou_expand()
    {
        var porFiltro = new Dictionary<string, StringValues>
        {
            ["$select"] = "Id,Nome",
            ["$filter"] = "contains(Email,'@')"
        };
        Assert.Equal(TipoRegra.Entidade, Resolver("GET", "/odata/Usuarios", porFiltro).Tipo);

        var porExpand = new Dictionary<string, StringValues>
        {
            ["$select"] = "Id,Nome",
            ["$expand"] = "Equipes"
        };
        Assert.Equal(TipoRegra.Entidade, Resolver("GET", "/odata/Usuarios", porExpand).Tipo);
    }

    [Fact]
    public void Usuario_so_troca_a_propria_senha_sem_permissao_extra()
    {
        Assert.Equal(TipoRegra.Livre, Resolver("POST", "/odata/Usuarios(7)/AlterarSenha", usuarioId: 7).Tipo);
        Assert.Equal(TipoRegra.Entidade, Resolver("POST", "/odata/Usuarios(8)/AlterarSenha", usuarioId: 7).Tipo);
    }

    [Fact]
    public void Consulta_de_relatorio_via_post_e_leitura_mas_importacao_grava()
    {
        Assert.Equal(AcaoPermissao.Ver, Resolver("POST", "/api/relatorios/consulta").Acao);
        Assert.Equal(AcaoPermissao.Criar, Resolver("POST", "/api/importacoes/executar").Acao);
    }
}

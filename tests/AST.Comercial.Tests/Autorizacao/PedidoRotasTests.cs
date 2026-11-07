using AST.Comercial.Domain.Autorizacao;
using AST.Comercial.Infrastructure.Autorizacao;
using Microsoft.AspNetCore.Http;

namespace AST.Comercial.Tests.Autorizacao;

/// <summary>Permissões das rotas de Pedidos (venda gerada a partir de proposta).</summary>
public class PedidoRotasTests
{
    private static RegraRota Resolver(string metodo, string caminho)
        => MapaPermissaoRotas.Resolver(metodo, new PathString(caminho), new QueryCollection(), null);

    [Theory]
    [InlineData("GET", "/odata/Pedidos", AcaoPermissao.Ver)]
    [InlineData("POST", "/odata/Pedidos", AcaoPermissao.Criar)]
    [InlineData("PATCH", "/odata/Pedidos(1)", AcaoPermissao.Editar)]
    [InlineData("DELETE", "/odata/Pedidos(1)", AcaoPermissao.Excluir)]
    public void Pedidos_seguem_o_verbo_sobre_a_entidade_Pedido(string metodo, string caminho, AcaoPermissao esperada)
    {
        var regra = Resolver(metodo, caminho);

        Assert.Equal(TipoRegra.Entidade, regra.Tipo);
        Assert.Equal(CatalogoEntidades.Pedido, regra.Entidade);
        Assert.Equal(esperada, regra.Acao);
    }

    [Fact]
    public void Gerar_pedido_de_proposta_exige_criar_pedido_e_nao_editar_proposta()
    {
        var regra = Resolver("POST", "/api/pedidos/de-proposta");

        Assert.Equal(CatalogoEntidades.Pedido, regra.Entidade);
        Assert.Equal(AcaoPermissao.Criar, regra.Acao);
    }

    [Fact]
    public void Itens_do_pedido_sao_filhos_do_pedido()
    {
        var regra = Resolver("POST", "/odata/ItensPedido");

        Assert.Equal(CatalogoEntidades.Pedido, regra.Entidade);
        Assert.Equal(AcaoPermissao.Editar, regra.Acao);
    }

    [Fact]
    public void Etapas_do_pedido_leem_livre_e_escrevem_como_cadastro_auxiliar()
    {
        Assert.Equal(TipoRegra.Livre, Resolver("GET", "/odata/EtapasPedido").Tipo);
        Assert.Equal(CatalogoEntidades.CadastroAuxiliar, Resolver("POST", "/odata/EtapasPedido").Entidade);
    }

    [Fact]
    public void Catalogo_de_permissoes_inclui_pedido_no_grupo_comercial()
    {
        var pedido = Assert.Single(CatalogoEntidades.Todas, e => e.Chave == CatalogoEntidades.Pedido);
        Assert.Equal(CatalogoEntidades.GrupoComercial, pedido.Grupo);
    }
}

using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Data;

/// <summary>
/// Importa campos personalizados de Produto inspirados na Ploomes.
/// Roda apenas se não existem campos personalizados de Produto para a empresa.
/// </summary>
public static class SeedCamposPloomesProduto
{
    public static async Task SeedAsync(AppDbContext db, long empresaId)
    {
        var temPersonalizados = await db.Campos
            .IgnoreQueryFilters()
            .AnyAsync(c => c.EmpresaId == empresaId && c.EntidadeAlvo == EntidadeAlvo.Produto && !c.Nativo);

        if (temPersonalizados) return;

        var campos = new List<Campo>
        {
            Criar(empresaId, "Descrição", "Produto_Descricao", TipoCampo.TextoLongo),
            Criar(empresaId, "Implantação", "Produto_Implantacao", TipoCampo.TextoLongo),
            Criar(empresaId, "Horário de Atendimento", "Produto_HorarioAtendimento", TipoCampo.TextoLongo),
            Criar(empresaId, "Importante", "Produto_Importante", TipoCampo.TextoLongo),
            Criar(empresaId, "Requisitos Mínimos", "Produto_RequisitosMinimos", TipoCampo.TextoLongo),
            Criar(empresaId, "Observações", "Produto_Observacoes", TipoCampo.TextoLongo),
            Criar(empresaId, "Notas", "Produto_Notas", TipoCampo.TextoLongo),
            Criar(empresaId, "Valor Licença", "Produto_ValorLicenca", TipoCampo.Moeda),
            Criar(empresaId, "Valor Adesão", "Produto_ValorAdesao", TipoCampo.Moeda),
            Criar(empresaId, "Adesão (Por conexão)", "Produto_AdesaoPorConexao", TipoCampo.Moeda),
            Criar(empresaId, "Valor Adesão (Não clientes)", "Produto_ValorAdesaoNaoClientes", TipoCampo.Moeda),
            Criar(empresaId, "Valor Licença (Não clientes)", "Produto_ValorLicencaNaoClientes", TipoCampo.Moeda),
            Criar(empresaId, "Quantidade mínima", "Produto_QuantidadeMinima", TipoCampo.Numero),
            Criar(empresaId, "Quantidade Módulos Individuais", "Produto_QtdModulosIndividuais", TipoCampo.Numero),
            Criar(empresaId, "Composição", "Produto_Composicao", TipoCampo.Texto),
            Criar(empresaId, "Composição Flexível", "Produto_ComposicaoFlexivel", TipoCampo.Booleano),
            Criar(empresaId, "Produto Composto", "Produto_ProdutoComposto", TipoCampo.Booleano),
            Criar(empresaId, "Tipo Precificação", "Produto_TipoPrecificacao", TipoCampo.Lista),
            Criar(empresaId, "Tipo de Movimento de Cobrança", "Produto_TipoMovCobranca", TipoCampo.Lista, multiplo: true),
            Criar(empresaId, "Tipo de Venda", "Produto_TipoVenda", TipoCampo.Lista, multiplo: true),
            Criar(empresaId, "Características", "Produto_Caracteristicas", TipoCampo.Lista, multiplo: true),
            Criar(empresaId, "Complemento do item", "Produto_ComplementoItem", TipoCampo.Lista),
            Criar(empresaId, "Tabela de preço", "Produto_TabelaPreco", TipoCampo.Texto),
            Criar(empresaId, "Composição do Produto", "Produto_ComposicaoProduto", TipoCampo.TextoLongo),
            Criar(empresaId, "URL Drive", "Produto_UrlDrive", TipoCampo.Texto),
        };

        db.Campos.AddRange(campos);
        await db.SaveChangesAsync();
    }

    private static Campo Criar(long empresaId, string nome, string chave, TipoCampo tipo, bool obrigatorio = false, bool multiplo = false)
    {
        return new Campo
        {
            Nome = nome,
            Chave = chave,
            Tipo = tipo,
            EntidadeAlvo = EntidadeAlvo.Produto,
            EmpresaId = empresaId,
            Nativo = false,
            Visivel = true,
            Ativo = true,
            Obrigatorio = obrigatorio,
            PermiteMultiplosValores = multiplo
        };
    }
}

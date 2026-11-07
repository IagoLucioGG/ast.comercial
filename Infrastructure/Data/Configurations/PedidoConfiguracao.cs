using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class PedidoConfiguracao : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Titulo).HasMaxLength(300);
        builder.Property(p => p.Observacoes).HasMaxLength(4000);
        builder.Property(p => p.FormaPagamento).HasMaxLength(200);
        builder.Property(p => p.Valor).HasPrecision(18, 2);
        builder.Property(p => p.Desconto).HasPrecision(18, 2);
        builder.Property(p => p.CustoFrete).HasPrecision(18, 2);

        builder.HasOne(p => p.Cliente).WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Negocio).WithMany().HasForeignKey(p => p.NegocioId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Proposta).WithMany().HasForeignKey(p => p.PropostaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Etapa).WithMany().HasForeignKey(p => p.EtapaPedidoId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.EmpresaId, p.Numero }).IsUnique();
        builder.HasIndex(p => p.PropostaId);
    }
}

public class ItemPedidoConfiguracao : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProdutoNome).HasMaxLength(300);
        builder.Property(i => i.ProdutoCodigo).HasMaxLength(100);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Property(i => i.Total).HasPrecision(18, 2);

        builder.HasOne(i => i.Pedido).WithMany(p => p.Itens).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.PedidoId, i.Ordem });
    }
}

public class EtapaPedidoConfiguracao : IEntityTypeConfiguration<EtapaPedido>
{
    public void Configure(EntityTypeBuilder<EtapaPedido> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Nome).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Cor).HasMaxLength(20);

        builder.HasIndex(e => new { e.EmpresaId, e.Ordem });
    }
}

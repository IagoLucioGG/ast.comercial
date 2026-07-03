using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ItemPropostaConfiguracao : IEntityTypeConfiguration<ItemProposta>
{
    public void Configure(EntityTypeBuilder<ItemProposta> builder)
    {
        
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProdutoNome).HasMaxLength(200);
        builder.Property(i => i.ProdutoCodigo).HasMaxLength(50);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Property(i => i.Total).HasPrecision(18, 2);

        builder.HasOne(i => i.Proposta)
            .WithMany(p => p.Produtos)
            .HasForeignKey(i => i.PropostaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Secao)
            .WithMany(s => s.Produtos)
            .HasForeignKey(i => i.SecaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.PropostaId, i.Ordem });
    }
}


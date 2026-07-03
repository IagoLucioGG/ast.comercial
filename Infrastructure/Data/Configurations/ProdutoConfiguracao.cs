using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Descricao).HasMaxLength(1000);
        builder.Property(p => p.Codigo).HasMaxLength(50);
        builder.Property(p => p.Unidade).HasMaxLength(20);
        builder.Property(p => p.Preco).HasPrecision(18, 2);
        builder.Property(p => p.PrecoCusto).HasPrecision(18, 2);

        builder.HasIndex(p => p.Codigo);
        builder.HasIndex(p => p.Nome);
    }
}





using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class FunilConfiguracao : IEntityTypeConfiguration<Funil>
{
    public void Configure(EntityTypeBuilder<Funil> builder)
    {
        
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nome).IsRequired().HasMaxLength(150);
        builder.Property(f => f.Descricao).HasMaxLength(500);
    }
}





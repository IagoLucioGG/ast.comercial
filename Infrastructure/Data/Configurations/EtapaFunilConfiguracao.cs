using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class EtapaFunilConfiguracao : IEntityTypeConfiguration<EtapaFunil>
{
    public void Configure(EntityTypeBuilder<EtapaFunil> builder)
    {
        
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nome).IsRequired().HasMaxLength(100);

        builder.HasOne(e => e.Funil)
            .WithMany(f => f.Etapas)
            .HasForeignKey(e => e.FunilId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.FunilId, e.Ordem });
    }
}





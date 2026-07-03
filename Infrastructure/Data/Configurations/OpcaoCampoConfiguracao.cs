using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class OpcaoCampoConfiguracao : IEntityTypeConfiguration<OpcaoCampo>
{
    public void Configure(EntityTypeBuilder<OpcaoCampo> builder)
    {
        
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Valor).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Rotulo).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Cor).HasMaxLength(20);

        builder.HasOne(o => o.Campo)
            .WithMany(c => c.Opcoes)
            .HasForeignKey(o => o.CampoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.CampoId, o.Ordem });
    }
}





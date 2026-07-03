using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class RegraCampoConfiguracao : IEntityTypeConfiguration<RegraCampo>
{
    public void Configure(EntityTypeBuilder<RegraCampo> builder)
    {
        
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ValorComparacao).HasColumnType("jsonb");
        builder.Property(r => r.FormulaCondicao).HasMaxLength(4000);

        builder.HasOne(r => r.Campo)
            .WithMany(c => c.Regras)
            .HasForeignKey(r => r.CampoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CampoOrigem)
            .WithMany()
            .HasForeignKey(r => r.CampoOrigemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CampoId);
    }
}





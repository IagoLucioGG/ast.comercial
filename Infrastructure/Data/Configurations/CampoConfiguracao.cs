using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class CampoConfiguracao : IEntityTypeConfiguration<Campo>
{
    public void Configure(EntityTypeBuilder<Campo> builder)
    {
        
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Chave).IsRequired().HasMaxLength(250);
        builder.Property(c => c.Descricao).HasMaxLength(500);
        builder.Property(c => c.Mascara).HasMaxLength(100);
        builder.Property(c => c.Formula).HasColumnType("text");
        builder.Property(c => c.FormulaVisibilidade).HasColumnType("text");
        builder.Property(c => c.FormulaObrigatoriedade).HasColumnType("text");
        builder.Property(c => c.FormulaSomenteLeitura).HasColumnType("text");
        builder.Property(c => c.ValorPadrao).HasColumnType("jsonb");
        builder.Property(c => c.ValorMinimo).HasPrecision(18, 4);
        builder.Property(c => c.ValorMaximo).HasPrecision(18, 4);

        builder.HasOne(c => c.EtapaFunil)
            .WithMany()
            .HasForeignKey(c => c.EtapaFunilId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.EntidadeAlvo, c.Chave }).IsUnique();
        builder.HasIndex(c => c.EntidadeAlvo);
    }
}





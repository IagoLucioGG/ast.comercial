using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ValorCampoConfiguracao : IEntityTypeConfiguration<ValorCampo>
{
    public void Configure(EntityTypeBuilder<ValorCampo> builder)
    {
        
        builder.HasKey(v => v.Id);

        builder.Property(v => v.CampoChave).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ValorTexto).HasMaxLength(500);
        builder.Property(v => v.ValorTextoGrande).HasColumnType("text");
        builder.Property(v => v.ValorDecimal).HasPrecision(18, 4);
        builder.Property(v => v.OpcaoValorNome).HasMaxLength(200);
        builder.Property(v => v.UsuarioValorNome).HasMaxLength(200);
        builder.Property(v => v.UsuarioValorAvatarUrl).HasMaxLength(500);
        builder.Property(v => v.ProdutoValorNome).HasMaxLength(200);
        builder.Property(v => v.ClienteValorNome).HasMaxLength(200);
        builder.Property(v => v.ClienteValorDocumento).HasMaxLength(20);
        builder.Property(v => v.AnexoValorNome).HasMaxLength(300);

        builder.HasOne(v => v.Campo)
            .WithMany()
            .HasForeignKey(v => v.CampoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.CampoId, v.RegistroId, v.EntidadeAlvo }).IsUnique();
        builder.HasIndex(v => new { v.RegistroId, v.EntidadeAlvo });
        builder.HasIndex(v => v.CampoChave);
        builder.HasIndex(v => v.ValorTexto);
        builder.HasIndex(v => v.ValorInteiro);
        builder.HasIndex(v => v.ValorDataHora);
    }
}


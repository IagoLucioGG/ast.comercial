using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
        builder.Property(u => u.SenhaHash).HasMaxLength(500);
        builder.Property(u => u.TokenIntegracao).HasMaxLength(500);
        builder.Property(u => u.DescricaoIntegracao).HasMaxLength(300);

        builder.HasOne(u => u.Empresa)
            .WithMany(e => e.Usuarios)
            .HasForeignKey(u => u.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.TokenIntegracao).IsUnique();
        builder.HasIndex(u => u.EmpresaId);
    }
}


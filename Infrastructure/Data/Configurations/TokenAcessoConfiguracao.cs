using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class TokenAcessoConfiguracao : IEntityTypeConfiguration<TokenAcesso>
{
    public void Configure(EntityTypeBuilder<TokenAcesso> builder)
    {
        
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).IsRequired().HasMaxLength(2000);
        builder.Property(t => t.RefreshToken).HasMaxLength(500);
        builder.Property(t => t.Origem).HasMaxLength(500);
        builder.Ignore(t => t.EstaValido);

        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.Tokens)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Token);
        builder.HasIndex(t => t.RefreshToken);
        builder.HasIndex(t => new { t.UsuarioId, t.Revogado });
    }
}


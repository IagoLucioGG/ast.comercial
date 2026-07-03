using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class PessoaContatoConfiguracao : IEntityTypeConfiguration<PessoaContato>
{
    public void Configure(EntityTypeBuilder<PessoaContato> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Email).HasMaxLength(150);
        builder.Property(p => p.Telefone).HasMaxLength(30);
        builder.Property(p => p.Cargo).HasMaxLength(100);
        builder.Property(p => p.Documento).HasMaxLength(20);

        builder.HasOne(p => p.Cliente)
            .WithMany(c => c.Contatos)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => p.Email);
        builder.HasIndex(p => p.Nome);
    }
}

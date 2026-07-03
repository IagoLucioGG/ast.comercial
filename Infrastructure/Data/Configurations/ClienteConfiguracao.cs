using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.RazaoSocial).HasMaxLength(300);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Telefone).HasMaxLength(30);
        builder.Property(c => c.Documento).HasMaxLength(20);
        builder.Property(c => c.Cidade).HasMaxLength(100);
        builder.Property(c => c.Estado).HasMaxLength(2);
        builder.Property(c => c.Endereco).HasMaxLength(300);
        builder.Property(c => c.Cep).HasMaxLength(10);
        builder.Property(c => c.Site).HasMaxLength(300);

        builder.HasOne(c => c.StatusCliente)
            .WithMany()
            .HasForeignKey(c => c.StatusClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Origem)
            .WithMany()
            .HasForeignKey(c => c.OrigemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.RamoAtividade)
            .WithMany()
            .HasForeignKey(c => c.RamoAtividadeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.PorteEmpresa)
            .WithMany()
            .HasForeignKey(c => c.PorteEmpresaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Documento);
        builder.HasIndex(c => c.Nome);
    }
}

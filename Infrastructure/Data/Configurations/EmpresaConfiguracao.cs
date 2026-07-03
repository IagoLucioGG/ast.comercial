using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class EmpresaConfiguracao : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nome).IsRequired().HasMaxLength(200);
        builder.Property(e => e.RazaoSocial).HasMaxLength(300);
        builder.Property(e => e.Cnpj).HasMaxLength(20);
        builder.Property(e => e.Telefone).HasMaxLength(30);
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.Endereco).HasMaxLength(300);
        builder.Property(e => e.Cidade).HasMaxLength(100);
        builder.Property(e => e.Estado).HasMaxLength(2);
        builder.Property(e => e.Cep).HasMaxLength(10);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);

        builder.HasIndex(e => e.Cnpj).IsUnique();
    }
}



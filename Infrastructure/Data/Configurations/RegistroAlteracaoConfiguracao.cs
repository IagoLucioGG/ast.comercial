using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class RegistroAlteracaoConfiguracao : IEntityTypeConfiguration<RegistroAlteracao>
{
    public void Configure(EntityTypeBuilder<RegistroAlteracao> builder)
    {
        
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Usuario).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Acao).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Titulo).HasMaxLength(500);
        builder.Property(r => r.DadosAntes).HasColumnType("jsonb");
        builder.Property(r => r.DadosDepois).HasColumnType("jsonb");

        builder.HasIndex(r => r.DataHora);
        builder.HasIndex(r => new { r.EntidadeAlvo, r.RegistroId });
        builder.HasIndex(r => r.Usuario);
    }
}





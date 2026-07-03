using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class DepartamentoConfiguracao : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> builder)
    {
        
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nome).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Descricao).HasMaxLength(300);

        builder.HasOne(d => d.DepartamentoPai)
            .WithMany(d => d.SubDepartamentos)
            .HasForeignKey(d => d.DepartamentoPaiId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => new { d.EmpresaId, d.Nome }).IsUnique();
    }
}


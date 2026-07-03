using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class FormularioConfiguracao : IEntityTypeConfiguration<Formulario>
{
    public void Configure(EntityTypeBuilder<Formulario> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).IsRequired().HasMaxLength(200);
        builder.HasIndex(f => new { f.EmpresaId, f.EntidadeAlvo, f.Nome }).IsUnique();
    }
}

public class SecaoFormularioConfiguracao : IEntityTypeConfiguration<SecaoFormulario>
{
    public void Configure(EntityTypeBuilder<SecaoFormulario> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Nome).IsRequired().HasMaxLength(200);

        builder.HasOne(s => s.Formulario)
            .WithMany(f => f.Secoes)
            .HasForeignKey(s => s.FormularioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.FormularioId, s.Ordem });
    }
}

public class CampoFormularioConfiguracao : IEntityTypeConfiguration<CampoFormulario>
{
    public void Configure(EntityTypeBuilder<CampoFormulario> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.NomeCampoFixo).HasMaxLength(100);
        builder.Property(c => c.Rotulo).HasMaxLength(200);
        builder.Property(c => c.Placeholder).HasMaxLength(200);
        builder.Property(c => c.Dica).HasMaxLength(500);
        builder.Property(c => c.Icone).HasMaxLength(50);
        builder.Property(c => c.FormulaVisibilidade).HasMaxLength(4000);
        builder.Property(c => c.FormulaObrigatoriedade).HasMaxLength(4000);
        builder.Property(c => c.FormulaCalculo).HasMaxLength(4000);
        builder.Property(c => c.FormulaValorPadrao).HasMaxLength(4000);
        builder.Property(c => c.FormulaSomenteLeitura).HasMaxLength(4000);

        builder.HasOne(c => c.Formulario)
            .WithMany(f => f.Campos)
            .HasForeignKey(c => c.FormularioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.SecaoFormulario)
            .WithMany(s => s.Campos)
            .HasForeignKey(c => c.SecaoFormularioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Campo)
            .WithMany()
            .HasForeignKey(c => c.CampoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.FormularioId, c.Ordem });
    }
}

using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ModeloDocumentoConfiguracao : IEntityTypeConfiguration<ModeloDocumento>
{
    public void Configure(EntityTypeBuilder<ModeloDocumento> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nome).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Descricao).HasMaxLength(1000);

        builder.HasOne(m => m.Equipe)
            .WithMany()
            .HasForeignKey(m => m.EquipeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.EmpresaId, m.EntidadeAlvo, m.Nome });
    }
}

public class SecaoModeloConfiguracao : IEntityTypeConfiguration<SecaoModelo>
{
    public void Configure(EntityTypeBuilder<SecaoModelo> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Nome).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ConteudoHtml).HasColumnType("text");

        builder.HasOne(s => s.ModeloDocumento)
            .WithMany(m => m.Secoes)
            .HasForeignKey(s => s.ModeloDocumentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ModeloDocumentoId, s.Ordem });
    }
}

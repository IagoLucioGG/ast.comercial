using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ConfiguracaoCampoConfiguracao : IEntityTypeConfiguration<ConfiguracaoCampo>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoCampo> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NomeCampoFixo).HasMaxLength(100);
        builder.Property(c => c.Rotulo).HasMaxLength(200);
        builder.Property(c => c.Placeholder).HasMaxLength(200);
        builder.Property(c => c.Dica).HasMaxLength(500);
        builder.Property(c => c.Agrupamento).HasMaxLength(100);
        builder.Property(c => c.FormulaVisibilidade).HasMaxLength(4000);
        builder.Property(c => c.FormulaObrigatoriedade).HasMaxLength(4000);
        builder.Property(c => c.FormulaCalculo).HasMaxLength(4000);
        builder.Property(c => c.FormulaValorPadrao).HasMaxLength(4000);
        builder.Property(c => c.FormulaSomenteLeitura).HasMaxLength(4000);

        builder.HasOne(c => c.Campo)
            .WithMany()
            .HasForeignKey(c => c.CampoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Funil)
            .WithMany()
            .HasForeignKey(c => c.FunilId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.EtapaFunil)
            .WithMany()
            .HasForeignKey(c => c.EtapaFunilId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.EmpresaId, c.EntidadeAlvo, c.CampoId, c.FunilId, c.EtapaFunilId, c.PropostaTemplateId });
        builder.HasIndex(c => new { c.EntidadeAlvo, c.NomeCampoFixo });
    }
}

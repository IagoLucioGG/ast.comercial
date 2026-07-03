using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class FilaAutomacaoConfiguracao : IEntityTypeConfiguration<FilaAutomacao>
{
    public void Configure(EntityTypeBuilder<FilaAutomacao> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UltimoErro).HasMaxLength(2000);
        builder.Property(f => f.DadosEvento).HasColumnType("jsonb");
        builder.Property(f => f.DisparadoPor).HasMaxLength(200);

        builder.HasOne(f => f.Automacao)
            .WithMany()
            .HasForeignKey(f => f.AutomacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.Status, f.ProximaTentativa, f.CriadoEm });
        builder.HasIndex(f => new { f.EmpresaId, f.Status });
        builder.HasIndex(f => new { f.AutomacaoId, f.Status });
        builder.HasIndex(f => new { f.EntidadeAlvo, f.RegistroId });
    }
}

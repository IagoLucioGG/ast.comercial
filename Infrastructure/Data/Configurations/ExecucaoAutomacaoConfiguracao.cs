using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ExecucaoAutomacaoConfiguracao : IEntityTypeConfiguration<ExecucaoAutomacao>
{
    public void Configure(EntityTypeBuilder<ExecucaoAutomacao> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Detalhes).HasColumnType("jsonb");
        builder.Property(e => e.Erro).HasMaxLength(2000);
        builder.Property(e => e.DisparadoPor).HasMaxLength(200);

        builder.HasOne(e => e.Automacao)
            .WithMany()
            .HasForeignKey(e => e.AutomacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EmpresaId, e.AutomacaoId, e.IniciadaEm });
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.EntidadeAlvo, e.RegistroId });
        builder.HasIndex(e => e.IniciadaEm);
    }
}

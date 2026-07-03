using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class FiltroAutomacaoConfiguracao : IEntityTypeConfiguration<FiltroAutomacao>
{
    public void Configure(EntityTypeBuilder<FiltroAutomacao> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.CampoReferencia).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Valor).HasMaxLength(1000);
        builder.Property(f => f.ValorAnterior).HasMaxLength(1000);

        builder.HasOne(f => f.Automacao)
            .WithMany(a => a.Filtros)
            .HasForeignKey(f => f.AutomacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.AutomacaoId, f.Grupo, f.Ordem });
    }
}

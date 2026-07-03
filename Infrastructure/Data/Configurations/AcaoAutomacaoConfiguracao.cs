using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class AcaoAutomacaoConfiguracao : IEntityTypeConfiguration<AcaoAutomacao>
{
    public void Configure(EntityTypeBuilder<AcaoAutomacao> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Configuracao).HasColumnType("jsonb");

        builder.HasOne(a => a.Automacao)
            .WithMany(au => au.Acoes)
            .HasForeignKey(a => a.AutomacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.AutomacaoId, a.Ordem });
    }
}

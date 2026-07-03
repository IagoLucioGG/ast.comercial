using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class ParcelaPropostaConfiguracao : IEntityTypeConfiguration<ParcelaProposta>
{
    public void Configure(EntityTypeBuilder<ParcelaProposta> builder)
    {
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Descricao).HasMaxLength(300);
        builder.Property(p => p.Valor).HasPrecision(18, 2);
        builder.Property(p => p.Percentual).HasPrecision(5, 2);

        builder.HasOne(p => p.Proposta)
            .WithMany(pr => pr.Parcelas)
            .HasForeignKey(p => p.PropostaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PropostaId, p.Numero });
    }
}


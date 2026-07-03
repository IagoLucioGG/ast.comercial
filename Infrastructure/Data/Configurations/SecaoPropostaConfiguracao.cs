using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class SecaoPropostaConfiguracao : IEntityTypeConfiguration<SecaoProposta>
{
    public void Configure(EntityTypeBuilder<SecaoProposta> builder)
    {
        
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nome).HasMaxLength(200);
        builder.Property(s => s.SubTotal).HasPrecision(18, 2);

        builder.HasOne(s => s.Proposta)
            .WithMany(p => p.Secoes)
            .HasForeignKey(s => s.PropostaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.PropostaId, s.Ordem });
    }
}


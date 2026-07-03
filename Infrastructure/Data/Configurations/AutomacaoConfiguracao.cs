using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class AutomacaoConfiguracao : IEntityTypeConfiguration<Automacao>
{
    public void Configure(EntityTypeBuilder<Automacao> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nome).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Descricao).HasMaxLength(500);
        builder.Property(a => a.GatilhoCampoReferencia).HasMaxLength(200);
        builder.Property(a => a.CronExpressao).HasMaxLength(100);
        builder.Property(a => a.FormulaCondicao).HasMaxLength(4000);

        builder.HasIndex(a => new { a.EmpresaId, a.EntidadeAlvo, a.Gatilho });
        builder.HasIndex(a => a.ProximaExecucao);
    }
}

using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class AtividadeConfiguracao : IEntityTypeConfiguration<Atividade>
{
    public void Configure(EntityTypeBuilder<Atividade> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Descricao).HasMaxLength(2000);

        builder.HasOne(a => a.Cliente)
            .WithMany(c => c.Atividades)
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.PessoaContato)
            .WithMany(p => p.Atividades)
            .HasForeignKey(a => a.PessoaContatoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Negocio)
            .WithMany(n => n.Atividades)
            .HasForeignKey(a => a.NegocioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.DataVencimento);
        builder.HasIndex(a => a.Concluida);
        builder.HasIndex(a => a.ClienteId);
        builder.HasIndex(a => a.PessoaContatoId);
        builder.HasIndex(a => a.NegocioId);
    }
}

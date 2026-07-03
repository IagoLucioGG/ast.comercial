using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class NegocioConfiguracao : IEntityTypeConfiguration<Negocio>
{
    public void Configure(EntityTypeBuilder<Negocio> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Titulo).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Valor).HasPrecision(18, 2);

        builder.HasOne(n => n.Cliente)
            .WithMany(c => c.Negocios)
            .HasForeignKey(n => n.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.PessoaContato)
            .WithMany()
            .HasForeignKey(n => n.PessoaContatoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Funil)
            .WithMany(f => f.Negocios)
            .HasForeignKey(n => n.FunilId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Etapa)
            .WithMany(e => e.Negocios)
            .HasForeignKey(n => n.EtapaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Status)
            .WithMany()
            .HasForeignKey(n => n.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.MotivoPerda)
            .WithMany()
            .HasForeignKey(n => n.MotivoPerdaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.StatusId);
        builder.HasIndex(n => n.ClienteId);
        builder.HasIndex(n => n.PessoaContatoId);
        builder.HasIndex(n => n.FunilId);
        builder.HasIndex(n => n.EtapaId);
    }
}

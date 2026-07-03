using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class FluxoAprovacaoConfiguracao : IEntityTypeConfiguration<FluxoAprovacao>
{
    public void Configure(EntityTypeBuilder<FluxoAprovacao> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).IsRequired().HasMaxLength(300);
        builder.Property(f => f.Mensagem).HasMaxLength(500);
        builder.HasIndex(f => new { f.EmpresaId, f.EntidadeAlvo, f.Ordem });
    }
}

public class FiltroFluxoAprovacaoConfiguracao : IEntityTypeConfiguration<FiltroFluxoAprovacao>
{
    public void Configure(EntityTypeBuilder<FiltroFluxoAprovacao> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.CampoReferencia).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Valor).HasMaxLength(1000);

        builder.HasOne(f => f.FluxoAprovacao)
            .WithMany(fl => fl.Filtros)
            .HasForeignKey(f => f.FluxoAprovacaoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NivelAprovacaoConfiguracao : IEntityTypeConfiguration<NivelAprovacao>
{
    public void Configure(EntityTypeBuilder<NivelAprovacao> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Descricao).HasMaxLength(200);

        builder.HasOne(n => n.FluxoAprovacao)
            .WithMany(f => f.Niveis)
            .HasForeignKey(n => n.FluxoAprovacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.FluxoAprovacaoId, n.Nivel });
    }
}

public class AprovadorNivelConfiguracao : IEntityTypeConfiguration<AprovadorNivel>
{
    public void Configure(EntityTypeBuilder<AprovadorNivel> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.NivelAprovacao)
            .WithMany(n => n.Aprovadores)
            .HasForeignKey(a => a.NivelAprovacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SolicitacaoAprovacaoConfiguracao : IEntityTypeConfiguration<SolicitacaoAprovacao>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAprovacao> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Observacao).HasMaxLength(1000);

        builder.HasOne(s => s.FluxoAprovacao)
            .WithMany()
            .HasForeignKey(s => s.FluxoAprovacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.AprovadoPor)
            .WithMany()
            .HasForeignKey(s => s.AprovadoPorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.EntidadeAlvo, s.RegistroId, s.Status });
        builder.HasIndex(s => s.Status);
    }
}

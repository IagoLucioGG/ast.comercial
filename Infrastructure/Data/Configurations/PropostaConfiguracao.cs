using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class PropostaConfiguracao : IEntityTypeConfiguration<Proposta>
{
    public void Configure(EntityTypeBuilder<Proposta> builder)
    {
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Titulo).HasMaxLength(300);
        builder.Property(p => p.Descricao).HasMaxLength(2000);
        builder.Property(p => p.Observacoes).HasMaxLength(2000);
        builder.Property(p => p.ModalFrete).HasMaxLength(100);
        builder.Property(p => p.PrazoEntrega).HasMaxLength(200);
        builder.Property(p => p.FormaPagamento).HasMaxLength(200);
        builder.Property(p => p.NomeArquivo).HasMaxLength(300);
        builder.Property(p => p.UrlDocumento).HasMaxLength(1000);
        builder.Property(p => p.Chave).HasMaxLength(200);
        builder.Property(p => p.Valor).HasPrecision(18, 2);
        builder.Property(p => p.Desconto).HasPrecision(18, 2);
        builder.Property(p => p.CustoFrete).HasPrecision(18, 2);
        builder.Property(p => p.CodigoFonteHeader).HasColumnType("text");
        builder.Property(p => p.CodigoFonteFooter).HasColumnType("text");
        builder.Property(p => p.CodigoFonteBody).HasColumnType("text");
        builder.Property(p => p.CodigoFontePreview).HasColumnType("text");
        builder.Property(p => p.CodigoFonteCapa).HasColumnType("text");

        builder.HasOne(p => p.Cliente)
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Negocio)
            .WithMany(n => n.Propostas)
            .HasForeignKey(p => p.NegocioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.NegocioId);
        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => p.IsTemplate);
        builder.HasIndex(p => p.StatusAprovacao);
        builder.HasIndex(p => new { p.EmpresaId, p.Numero });
        builder.HasIndex(p => p.Chave).IsUnique();
    }
}


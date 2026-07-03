using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class MembroEquipeConfiguracao : IEntityTypeConfiguration<MembroEquipe>
{
    public void Configure(EntityTypeBuilder<MembroEquipe> builder)
    {
        
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Equipe)
            .WithMany(e => e.Membros)
            .HasForeignKey(m => m.EquipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.Equipes)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.EquipeId, m.UsuarioId }).IsUnique();
    }
}


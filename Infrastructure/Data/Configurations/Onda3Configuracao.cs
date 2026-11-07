using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class PermissaoPerfilConfiguracao : IEntityTypeConfiguration<PermissaoPerfil>
{
    public void Configure(EntityTypeBuilder<PermissaoPerfil> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Entidade).IsRequired().HasMaxLength(60);
        builder.HasIndex(p => new { p.EmpresaId, p.Perfil, p.Entidade }).IsUnique();
    }
}

public class ItemNegocioConfiguracao : IEntityTypeConfiguration<ItemNegocio>
{
    public void Configure(EntityTypeBuilder<ItemNegocio> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Descricao).IsRequired().HasMaxLength(300);
        builder.Property(i => i.Quantidade).HasPrecision(18, 4);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Property(i => i.Desconto).HasPrecision(5, 2);
        builder.Property(i => i.Total).HasPrecision(18, 2);

        builder.HasOne(i => i.Negocio)
            .WithMany(n => n.Itens)
            .HasForeignKey(i => i.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.NegocioId, i.Ordem });
        builder.HasIndex(i => i.ProdutoId);
    }
}

public class ListaPrecoConfiguracao : IEntityTypeConfiguration<ListaPreco>
{
    public void Configure(EntityTypeBuilder<ListaPreco> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Nome).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Descricao).HasMaxLength(1000);

        builder.HasOne(l => l.Moeda)
            .WithMany()
            .HasForeignKey(l => l.MoedaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.EmpresaId, l.Nome });
    }
}

public class ItemListaPrecoConfiguracao : IEntityTypeConfiguration<ItemListaPreco>
{
    public void Configure(EntityTypeBuilder<ItemListaPreco> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Preco).HasPrecision(18, 2);

        builder.HasOne(i => i.ListaPreco)
            .WithMany(l => l.Itens)
            .HasForeignKey(i => i.ListaPrecoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Produto)
            .WithMany(p => p.ItensListaPreco)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.ListaPrecoId, i.ProdutoId }).IsUnique();
    }
}

public class ChecklistEtapaConfiguracao : IEntityTypeConfiguration<ChecklistEtapa>
{
    public void Configure(EntityTypeBuilder<ChecklistEtapa> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);

        builder.HasOne(c => c.EtapaFunil)
            .WithMany(e => e.Checklists)
            .HasForeignKey(c => c.EtapaFunilId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.EtapaFunilId, c.Ordem });
    }
}

public class ItemChecklistEtapaConfiguracao : IEntityTypeConfiguration<ItemChecklistEtapa>
{
    public void Configure(EntityTypeBuilder<ItemChecklistEtapa> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Texto).IsRequired().HasMaxLength(500);

        builder.HasOne(i => i.ChecklistEtapa)
            .WithMany(c => c.Itens)
            .HasForeignKey(i => i.ChecklistEtapaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.ChecklistEtapaId, i.Ordem });
    }
}

public class RespostaChecklistNegocioConfiguracao : IEntityTypeConfiguration<RespostaChecklistNegocio>
{
    public void Configure(EntityTypeBuilder<RespostaChecklistNegocio> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Negocio)
            .WithMany(n => n.RespostasChecklist)
            .HasForeignKey(r => r.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ItemChecklistEtapa)
            .WithMany()
            .HasForeignKey(r => r.ItemChecklistEtapaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ConcluidoPorUsuario)
            .WithMany()
            .HasForeignKey(r => r.ConcluidoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.NegocioId, r.ItemChecklistEtapaId }).IsUnique();
    }
}

public class MetaConfiguracao : IEntityTypeConfiguration<Meta>
{
    public void Configure(EntityTypeBuilder<Meta> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nome).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Alvo).HasPrecision(18, 2);

        builder.HasOne(m => m.Moeda).WithMany().HasForeignKey(m => m.MoedaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Equipe).WithMany().HasForeignKey(m => m.EquipeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Funil).WithMany().HasForeignKey(m => m.FunilId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.EmpresaId, m.Inicio, m.Fim });
        builder.HasIndex(m => m.UsuarioId);
        builder.HasIndex(m => m.EquipeId);
    }
}

public class PainelRelatorioConfiguracao : IEntityTypeConfiguration<PainelRelatorio>
{
    public void Configure(EntityTypeBuilder<PainelRelatorio> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Descricao).HasMaxLength(1000);

        builder.HasOne(p => p.CriadoPorUsuario)
            .WithMany()
            .HasForeignKey(p => p.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.EmpresaId, p.Ordem });
    }
}

public class GraficoRelatorioConfiguracao : IEntityTypeConfiguration<GraficoRelatorio>
{
    public void Configure(EntityTypeBuilder<GraficoRelatorio> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Entidade).IsRequired().HasMaxLength(60);
        builder.Property(g => g.CampoValor).HasMaxLength(100);
        builder.Property(g => g.CampoAgrupamento).HasMaxLength(100);
        builder.Property(g => g.PeriodoCampo).HasMaxLength(100);
        builder.Property(g => g.PeriodoRelativo).HasMaxLength(30);
        builder.Property(g => g.FiltroJson).HasColumnType("text");
        builder.Property(g => g.Largura).HasDefaultValue(2);

        builder.ToTable(t => t.HasCheckConstraint("CK_GraficoRelatorio_Largura", "\"Largura\" BETWEEN 1 AND 4"));

        builder.HasOne(g => g.PainelRelatorio)
            .WithMany(p => p.Graficos)
            .HasForeignKey(g => g.PainelRelatorioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.PainelRelatorioId, g.Ordem });
    }
}

public class ImportacaoConfiguracao : IEntityTypeConfiguration<Importacao>
{
    public void Configure(EntityTypeBuilder<Importacao> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Entidade).IsRequired().HasMaxLength(60);
        builder.Property(i => i.NomeArquivo).IsRequired().HasMaxLength(300);
        builder.Property(i => i.MapeamentoJson).HasColumnType("text");
        builder.Property(i => i.ErrosJson).HasColumnType("text");

        builder.HasOne(i => i.Usuario)
            .WithMany()
            .HasForeignKey(i => i.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.EmpresaId, i.CriadoEm });
    }
}

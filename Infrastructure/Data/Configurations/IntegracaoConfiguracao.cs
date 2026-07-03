using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AST.Comercial.Infrastructure.Data.Configurations;

public class IntegracaoConfiguracao : IEntityTypeConfiguration<Integracao>
{
    public void Configure(EntityTypeBuilder<Integracao> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Nome).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Provedor).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Descricao).HasMaxLength(1000);
        builder.Property(i => i.LogoUrl).HasMaxLength(500);
        builder.Property(i => i.ApiUrl).HasMaxLength(500);
        builder.Property(i => i.ApiToken).HasMaxLength(2000);
        builder.Property(i => i.AuthUsuario).HasMaxLength(200);
        builder.Property(i => i.AuthSenha).HasMaxLength(500);
        builder.Property(i => i.ClientId).HasMaxLength(200);
        builder.Property(i => i.ClientSecret).HasMaxLength(500);
        builder.Property(i => i.OAuthTokenUrl).HasMaxLength(500);
        builder.Property(i => i.OAuthScope).HasMaxLength(200);
        builder.Property(i => i.ApiKeyHeader).HasMaxLength(100);
        builder.Property(i => i.ApiKeyValor).HasMaxLength(500);
        builder.Property(i => i.HeadersFixos).HasColumnType("jsonb");
        builder.Property(i => i.WebhookSecret).HasMaxLength(200);
        builder.Property(i => i.ChaveWebhook).HasMaxLength(100);
        builder.Property(i => i.Configuracao).HasColumnType("jsonb");

        builder.HasIndex(i => new { i.EmpresaId, i.Provedor });
        builder.HasIndex(i => i.ChaveWebhook).IsUnique();
    }
}

public class FluxoIntegracaoConfiguracao : IEntityTypeConfiguration<FluxoIntegracao>
{
    public void Configure(EntityTypeBuilder<FluxoIntegracao> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).IsRequired().HasMaxLength(200);
        builder.Property(f => f.EventoGatilho).HasMaxLength(100);
        builder.Property(f => f.EndpointSaida).HasMaxLength(500);
        builder.Property(f => f.MetodoHttpSaida).HasMaxLength(10);
        builder.Property(f => f.TemplateBody).HasColumnType("text");
        builder.Property(f => f.WebhookCampoIdentificador).HasMaxLength(100);
        builder.Property(f => f.WebhookValorIdentificador).HasMaxLength(200);
        builder.Property(f => f.EndpointPolling).HasMaxLength(500);
        builder.Property(f => f.MetodoHttpPolling).HasMaxLength(10);
        builder.Property(f => f.CampoListaResponse).HasMaxLength(100);
        builder.Property(f => f.CampoPaginacaoResponse).HasMaxLength(100);
        builder.Property(f => f.CampoChaveDuplicidade).HasMaxLength(100);
        builder.Property(f => f.Configuracao).HasColumnType("jsonb");

        builder.HasOne(f => f.Integracao)
            .WithMany(i => i.Fluxos)
            .HasForeignKey(f => f.IntegracaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.IntegracaoId, f.Direcao });
    }
}

public class MapeamentoCampoIntegracaoConfiguracao : IEntityTypeConfiguration<MapeamentoCampoIntegracao>
{
    public void Configure(EntityTypeBuilder<MapeamentoCampoIntegracao> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CampoInterno).IsRequired().HasMaxLength(200);
        builder.Property(m => m.CampoExterno).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Transformacao).HasMaxLength(500);
        builder.Property(m => m.ValorPadrao).HasMaxLength(500);

        builder.HasOne(m => m.Integracao)
            .WithMany(i => i.Mapeamentos)
            .HasForeignKey(m => m.IntegracaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.FluxoIntegracao)
            .WithMany()
            .HasForeignKey(m => m.FluxoIntegracaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.IntegracaoId, m.FluxoIntegracaoId, m.Ordem });
    }
}

public class LogIntegracaoConfiguracao : IEntityTypeConfiguration<LogIntegracao>
{
    public void Configure(EntityTypeBuilder<LogIntegracao> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Endpoint).HasMaxLength(500);
        builder.Property(l => l.MetodoHttp).HasMaxLength(10);
        builder.Property(l => l.RequestBody).HasColumnType("text");
        builder.Property(l => l.ResponseBody).HasColumnType("text");
        builder.Property(l => l.Erro).HasMaxLength(2000);

        builder.HasOne(l => l.Integracao)
            .WithMany()
            .HasForeignKey(l => l.IntegracaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.FluxoIntegracao)
            .WithMany()
            .HasForeignKey(l => l.FluxoIntegracaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.IntegracaoId, l.CriadoEm });
        builder.HasIndex(l => l.Sucesso);
    }
}

public class EtapaFluxoIntegracaoConfiguracao : IEntityTypeConfiguration<EtapaFluxoIntegracao>
{
    public void Configure(EntityTypeBuilder<EtapaFluxoIntegracao> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Nome).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Endpoint).IsRequired().HasMaxLength(500);
        builder.Property(e => e.MetodoHttp).HasMaxLength(10);
        builder.Property(e => e.CampoChaveConsulta).HasMaxLength(200);
        builder.Property(e => e.CampoInternoChave).HasMaxLength(200);
        builder.Property(e => e.ArmazenarResultadoComo).HasMaxLength(100);
        builder.Property(e => e.CampoResultadoResponse).HasMaxLength(100);
        builder.Property(e => e.CondicaoCampo).HasMaxLength(200);
        builder.Property(e => e.CondicaoValor).HasMaxLength(500);
        builder.Property(e => e.TemplateBody).HasColumnType("text");
        builder.Property(e => e.Configuracao).HasColumnType("jsonb");

        builder.HasOne(e => e.FluxoIntegracao)
            .WithMany(f => f.Etapas)
            .HasForeignKey(e => e.FluxoIntegracaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.FluxoIntegracaoId, e.Ordem });
    }
}

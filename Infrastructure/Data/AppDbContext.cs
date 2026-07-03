using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AST.Comercial.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Usuário atual (definido pelo middleware/controller antes de salvar)
    /// </summary>
    public string UsuarioAtual { get; set; } = "Sistema";

    /// <summary>
    /// Empresa do usuário logado (define o tenant para filtro global)
    /// </summary>
    public long EmpresaIdAtual { get; set; }
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<PessoaContato> PessoasContato => Set<PessoaContato>();
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Funil> Funis => Set<Funil>();
    public DbSet<EtapaFunil> EtapasFunil => Set<EtapaFunil>();
    public DbSet<Atividade> Atividades => Set<Atividade>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Proposta> Propostas => Set<Proposta>();
    public DbSet<ItemProposta> ItensProposta => Set<ItemProposta>();
    public DbSet<SecaoProposta> SecoesProposta => Set<SecaoProposta>();
    public DbSet<ParcelaProposta> ParcelasProposta => Set<ParcelaProposta>();
    public DbSet<StatusNegocio> StatusNegocios => Set<StatusNegocio>();
    public DbSet<StatusCliente> StatusClientes => Set<StatusCliente>();
    public DbSet<MotivoPerda> MotivosPerda => Set<MotivoPerda>();
    public DbSet<OrigemContato> OrigensContato => Set<OrigemContato>();
    public DbSet<RamoAtividade> RamosAtividade => Set<RamoAtividade>();
    public DbSet<PorteEmpresa> PortesEmpresa => Set<PorteEmpresa>();
    public DbSet<FamiliaProduto> FamiliasProduto => Set<FamiliaProduto>();
    public DbSet<GrupoProduto> GruposProduto => Set<GrupoProduto>();
    public DbSet<Moeda> Moedas => Set<Moeda>();
    public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();
    public DbSet<EtiquetaRegistro> EtiquetasRegistro => Set<EtiquetaRegistro>();
    public DbSet<Localidade> Localidades => Set<Localidade>();
    public DbSet<FluxoAprovacao> FluxosAprovacao => Set<FluxoAprovacao>();
    public DbSet<FiltroFluxoAprovacao> FiltrosFluxoAprovacao => Set<FiltroFluxoAprovacao>();
    public DbSet<NivelAprovacao> NiveisAprovacao => Set<NivelAprovacao>();
    public DbSet<AprovadorNivel> AprovadoresNivel => Set<AprovadorNivel>();
    public DbSet<SolicitacaoAprovacao> SolicitacoesAprovacao => Set<SolicitacaoAprovacao>();
    public DbSet<Integracao> Integracoes => Set<Integracao>();
    public DbSet<MapeamentoCampoIntegracao> MapeamentosIntegracao => Set<MapeamentoCampoIntegracao>();
    public DbSet<FluxoIntegracao> FluxosIntegracao => Set<FluxoIntegracao>();
    public DbSet<LogIntegracao> LogsIntegracao => Set<LogIntegracao>();
    public DbSet<EtapaFluxoIntegracao> EtapasFluxoIntegracao => Set<EtapaFluxoIntegracao>();
    public DbSet<Automacao> Automacoes => Set<Automacao>();
    public DbSet<FiltroAutomacao> FiltrosAutomacao => Set<FiltroAutomacao>();
    public DbSet<AcaoAutomacao> AcoesAutomacao => Set<AcaoAutomacao>();
    public DbSet<ExecucaoAutomacao> ExecucoesAutomacao => Set<ExecucaoAutomacao>();
    public DbSet<FilaAutomacao> FilaAutomacao => Set<FilaAutomacao>();
    public DbSet<Campo> Campos => Set<Campo>();
    public DbSet<OpcaoCampo> OpcoesCampo => Set<OpcaoCampo>();
    public DbSet<RegraCampo> RegrasCampo => Set<RegraCampo>();
    public DbSet<ValorCampo> ValoresCampo => Set<ValorCampo>();
    public DbSet<ConfiguracaoCampo> ConfiguracoesCampo => Set<ConfiguracaoCampo>();
    public DbSet<Formulario> Formularios => Set<Formulario>();
    public DbSet<SecaoFormulario> SecoesFormulario => Set<SecaoFormulario>();
    public DbSet<CampoFormulario> CamposFormulario => Set<CampoFormulario>();
    public DbSet<RegistroAlteracao> RegistrosAlteracao => Set<RegistroAlteracao>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenAcesso> TokensAcesso => Set<TokenAcesso>();
    public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<MembroEquipe> MembrosEquipe => Set<MembroEquipe>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<VisualizacaoSalva> VisualizacoesSalvas => Set<VisualizacaoSalva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Nome das tabelas = nome da classe da entidade
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.ClrType.Name);
        }

        // Salvar todos os enums como string no banco
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (clrType.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));
                }
            }
        }

        // Global query filter: isolamento por empresa (multi-tenant)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(EntidadeBase)) && entityType.ClrType != typeof(Empresa))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var empresaIdProp = System.Linq.Expressions.Expression.Property(parameter, nameof(EntidadeBase.EmpresaId));
                var empresaIdAtual = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(this), nameof(EmpresaIdAtual));
                var ativoProp = System.Linq.Expressions.Expression.Property(parameter, nameof(EntidadeBase.Ativo));

                var filtroEmpresa = System.Linq.Expressions.Expression.Equal(empresaIdProp, empresaIdAtual);
                var filtroAtivo = System.Linq.Expressions.Expression.Equal(ativoProp, System.Linq.Expressions.Expression.Constant(true));
                var filtroCompleto = System.Linq.Expressions.Expression.AndAlso(filtroEmpresa, filtroAtivo);

                var lambda = System.Linq.Expressions.Expression.Lambda(filtroCompleto, parameter);
                entityType.SetQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Atualizar timestamps
        foreach (var entry in ChangeTracker.Entries<EntidadeBase>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.AtualizadoEm = DateTime.UtcNow;
        }

        // Agrupar alterações por entidade raiz
        var alteracoesPorRaiz = new Dictionary<(EntidadeAlvo, long), List<object>>();
        var estadosRaiz = new Dictionary<(EntidadeAlvo, long), EntityState>();

        foreach (var entry in ChangeTracker.Entries<EntidadeBase>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var (entidadeAlvo, registroId) = ResolverRaiz(entry.Entity);
            if (entidadeAlvo is null) continue;

            var chave = (entidadeAlvo.Value, registroId);

            if (!alteracoesPorRaiz.ContainsKey(chave))
            {
                alteracoesPorRaiz[chave] = [];
                estadosRaiz[chave] = entry.State;
            }

            // A entidade raiz define o estado principal do log
            if (EhEntidadeRaiz(entry.Entity))
                estadosRaiz[chave] = entry.State;

            alteracoesPorRaiz[chave].Add(entry.Entity);
        }

        // Gerar um RegistroAlteracao por entidade raiz
        var registros = new List<RegistroAlteracao>();
        foreach (var (chave, entidades) in alteracoesPorRaiz)
        {
            var (entidadeAlvo, registroId) = chave;
            var estado = estadosRaiz[chave];
            var raiz = entidades.FirstOrDefault(e => EhEntidadeRaiz((EntidadeBase)e)) as EntidadeBase;

            string acao = estado switch
            {
                EntityState.Added => "Criacao",
                EntityState.Modified => "Alteracao",
                EntityState.Deleted => "Exclusao",
                _ => "Alteracao"
            };

            // Snapshot antes (valores originais dos campos modificados na raiz)
            string? dadosAntes = null;
            string? dadosDepois = null;

            if (raiz is not null)
            {
                var entryRaiz = Entry(raiz);
                if (estado == EntityState.Modified || estado == EntityState.Deleted)
                    dadosAntes = SerializarValoresOriginais(entryRaiz);

                if (estado != EntityState.Deleted)
                    dadosDepois = SerializarSnapshotCompleto(raiz, entidades);
            }
            else
            {
                // Alteração apenas em filhas (ex: ValorCampo alterado sem alterar a raiz)
                dadosDepois = JsonSerializer.Serialize(entidades, SerializerOpcoes);
            }

            registros.Add(new RegistroAlteracao
            {
                Usuario = UsuarioAtual,
                Acao = acao,
                EntidadeAlvo = entidadeAlvo,
                RegistroId = registroId,
                Titulo = raiz is not null ? ObterTitulo(raiz) : null,
                DadosAntes = dadosAntes,
                DadosDepois = dadosDepois
            });
        }

        if (registros.Count > 0)
            RegistrosAlteracao.AddRange(registros);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static readonly JsonSerializerOptions SerializerOpcoes = new()
    {
        WriteIndented = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    private static bool EhEntidadeRaiz(object entidade)
        => entidade is Cliente or PessoaContato or Negocio or Funil or EtapaFunil or Atividade or Produto or Campo or Empresa or Usuario or Equipe or Departamento or Cargo;

    /// <summary>
    /// Resolve qual é a entidade raiz e seu Id.
    /// Entidades filhas apontam para a raiz via RegistroId/NegocioId.
    /// </summary>
    private static (EntidadeAlvo?, long) ResolverRaiz(EntidadeBase entidade) => entidade switch
    {
        Cliente c => (Domain.Entidades.EntidadeAlvo.Cliente, c.Id),
        PessoaContato pc => (Domain.Entidades.EntidadeAlvo.PessoaContato, pc.Id),
        Negocio n => (Domain.Entidades.EntidadeAlvo.Negocio, n.Id),
        Funil f => (Domain.Entidades.EntidadeAlvo.Funil, f.Id),
        EtapaFunil e => (Domain.Entidades.EntidadeAlvo.EtapaFunil, e.Id),
        Atividade a => (Domain.Entidades.EntidadeAlvo.Atividade, a.Id),
        Produto p => (Domain.Entidades.EntidadeAlvo.Produto, p.Id),
        Empresa em => (Domain.Entidades.EntidadeAlvo.Empresa, em.Id),
        Usuario u => (Domain.Entidades.EntidadeAlvo.Usuario, u.Id),
        Equipe eq => (Domain.Entidades.EntidadeAlvo.Equipe, eq.Id),
        Departamento d => (Domain.Entidades.EntidadeAlvo.Departamento, d.Id),
        Cargo ca => (Domain.Entidades.EntidadeAlvo.Cargo, ca.Id),
        MembroEquipe me => (Domain.Entidades.EntidadeAlvo.Equipe, me.EquipeId),
        Campo c => (c.EntidadeAlvo, c.Id),
        ValorCampo v => (v.EntidadeAlvo, v.RegistroId),
        Proposta pr => (Domain.Entidades.EntidadeAlvo.Negocio, pr.NegocioId ?? pr.Id),
        ItemProposta ip => (Domain.Entidades.EntidadeAlvo.Negocio, ip.PropostaId),
        OpcaoCampo => (null, 0),
        RegraCampo => (null, 0),
        _ => (null, 0)
    };

    private static string? ObterTitulo(EntidadeBase entidade) => entidade switch
    {
        Cliente c => c.Nome,
        PessoaContato pc => pc.Nome,
        Negocio n => n.Titulo,
        Funil f => f.Nome,
        EtapaFunil e => e.Nome,
        Atividade a => a.Titulo,
        Produto p => p.Nome,
        Campo c => c.Nome,
        Empresa em => em.Nome,
        Usuario u => u.Nome,
        Equipe eq => eq.Nome,
        Departamento d => d.Nome,
        Cargo ca => ca.Nome,
        _ => null
    };

    private static string SerializarSnapshotCompleto(EntidadeBase raiz, List<object> entidades)
    {
        var relacionados = entidades.Where(e => e != raiz).ToList();
        if (relacionados.Count == 0)
            return JsonSerializer.Serialize(raiz, SerializerOpcoes);

        var snapshot = new Dictionary<string, object?>
        {
            ["Entidade"] = raiz,
            ["Relacionados"] = relacionados
        };
        return JsonSerializer.Serialize(snapshot, SerializerOpcoes);
    }

    private static string SerializarValoresOriginais(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var valores = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            valores[prop.Metadata.Name] = prop.OriginalValue;
        }
        return JsonSerializer.Serialize(valores, SerializerOpcoes);
    }
}






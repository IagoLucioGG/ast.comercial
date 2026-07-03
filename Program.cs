using System.Text;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Application.Servicos;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using AST.Comercial.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

// EF Core + PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Chave"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// CORS restritivo
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// Desabilitar header Server do Kestrel + limitar tamanho do request body
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB máximo
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB headers
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Serviços
builder.Services.AddScoped<IClienteServico, ClienteServico>();
builder.Services.AddScoped<IPessoaContatoServico, PessoaContatoServico>();
builder.Services.AddScoped<INegocioServico, NegocioServico>();
builder.Services.AddScoped<IFunilServico, FunilServico>();
builder.Services.AddScoped<IAtividadeServico, AtividadeServico>();
builder.Services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
builder.Services.AddScoped<IPropostaServico, PropostaServico>();
builder.Services.AddScoped<IProdutoServico, ProdutoServico>();
builder.Services.AddScoped<ICampoServico, CampoServico>();
builder.Services.AddScoped<IAutomacaoServico, AutomacaoServico>();
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();
builder.Services.AddScoped<IEmpresaServico, EmpresaServico>();
builder.Services.AddScoped<IEquipeServico, EquipeServico>();
builder.Services.AddScoped<IDepartamentoServico, DepartamentoServico>();
builder.Services.AddScoped<ICargoServico, CargoServico>();
builder.Services.AddScoped<IFormularioServico, FormularioServico>();
builder.Services.AddScoped<IFluxoAprovacaoServico, FluxoAprovacaoServico>();
builder.Services.AddScoped<IIntegracaoServico, IntegracaoServico>();
builder.Services.AddScoped<IIntegracaoEventoPublicador, AST.Comercial.Infrastructure.Integracoes.IntegracaoEventoPublicador>();
builder.Services.AddScoped<AST.Comercial.Infrastructure.Integracoes.ExecutorFluxoSaida>();
builder.Services.AddScoped<IEtiquetaServico, EtiquetaServico>();
builder.Services.AddScoped<IMoedaServico, MoedaServico>();
builder.Services.AddScoped<ILocalidadeServico, LocalidadeServico>();
builder.Services.AddScoped<IFilaAutomacaoPublicador, AST.Comercial.Infrastructure.Automacoes.FilaAutomacaoPublicador>();
builder.Services.AddScoped<IMotorAutomacao, AST.Comercial.Infrastructure.Automacoes.MotorAutomacao>();
builder.Services.AddHostedService<AST.Comercial.Infrastructure.Automacoes.ProcessadorAutomacaoService>();
builder.Services.AddHostedService<AST.Comercial.Infrastructure.Automacoes.AgendadorAutomacaoService>();
builder.Services.AddHostedService<AST.Comercial.Infrastructure.Integracoes.PollingIntegracaoService>();

// OData EDM Model
var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EntitySet<Cliente>("Clientes");
modelBuilder.EntitySet<PessoaContato>("PessoasContato");
modelBuilder.EntitySet<Negocio>("Negocios");
modelBuilder.EntitySet<Funil>("Funis");
modelBuilder.EntitySet<EtapaFunil>("EtapasFunil");
modelBuilder.EntitySet<Atividade>("Atividades");
modelBuilder.EntitySet<Produto>("Produtos");
modelBuilder.EntitySet<Campo>("Campos");
modelBuilder.EntitySet<OpcaoCampo>("OpcoesCampo");
modelBuilder.EntitySet<RegraCampo>("RegrasCampo");
modelBuilder.EntitySet<ValorCampo>("ValoresCampo");
modelBuilder.EntitySet<ConfiguracaoCampo>("ConfiguracoesCampo");
modelBuilder.EntitySet<Formulario>("Formularios");
modelBuilder.EntitySet<SecaoFormulario>("SecoesFormulario");
modelBuilder.EntitySet<CampoFormulario>("CamposFormulario");
modelBuilder.EntitySet<Proposta>("Propostas");
modelBuilder.EntitySet<ItemProposta>("ItensProposta");
modelBuilder.EntitySet<SecaoProposta>("SecoesProposta");
modelBuilder.EntitySet<ParcelaProposta>("ParcelasProposta");
modelBuilder.EntitySet<RegistroAlteracao>("RegistrosAlteracao");
modelBuilder.EntitySet<Empresa>("Empresas");
modelBuilder.EntityType<Empresa>().HasKey(e => e.Id);
modelBuilder.EntitySet<Usuario>("Usuarios");
modelBuilder.EntitySet<Equipe>("Equipes");
modelBuilder.EntitySet<MembroEquipe>("MembrosEquipe");
modelBuilder.EntitySet<Departamento>("Departamentos");
modelBuilder.EntitySet<Cargo>("Cargos");
modelBuilder.EntitySet<StatusNegocio>("StatusNegocios");
modelBuilder.EntitySet<StatusCliente>("StatusClientes");
modelBuilder.EntitySet<MotivoPerda>("MotivosPerda");
modelBuilder.EntitySet<OrigemContato>("OrigensContato");
modelBuilder.EntitySet<RamoAtividade>("RamosAtividade");
modelBuilder.EntitySet<PorteEmpresa>("PortesEmpresa");
modelBuilder.EntitySet<FamiliaProduto>("FamiliasProduto");
modelBuilder.EntitySet<GrupoProduto>("GruposProduto");
modelBuilder.EntitySet<Moeda>("Moedas");
modelBuilder.EntitySet<Etiqueta>("Etiquetas");
modelBuilder.EntitySet<EtiquetaRegistro>("EtiquetasRegistro");
modelBuilder.EntitySet<Localidade>("Localidades");
modelBuilder.EntitySet<FluxoAprovacao>("FluxosAprovacao");
modelBuilder.EntitySet<SolicitacaoAprovacao>("SolicitacoesAprovacao");
modelBuilder.EntitySet<Integracao>("Integracoes");
modelBuilder.EntitySet<LogIntegracao>("LogsIntegracao");
modelBuilder.EntitySet<Automacao>("Automacoes");
modelBuilder.EntitySet<FiltroAutomacao>("FiltrosAutomacao");
modelBuilder.EntitySet<AcaoAutomacao>("AcoesAutomacao");
modelBuilder.EntitySet<ExecucaoAutomacao>("ExecucoesAutomacao");
modelBuilder.EntitySet<FilaAutomacao>("FilaAutomacao");
modelBuilder.EntitySet<VisualizacaoSalva>("Visualizacoes");

// Controllers + OData (autenticação global obrigatória)
builder.Services.AddControllers(options =>
    {
        var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
    })
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100)
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi().AllowAnonymous();

app.UseStaticFiles();

app.MapGet("/odata", () => Results.Json(new
{
    documentacao = "/index.html",
    metadata = "/odata/$metadata",
    entidades = new[]
    {
        "Clientes", "PessoasContato", "Negocios", "Propostas", "Produtos",
        "Atividades", "Funis", "Campos", "Formularios", "Automacoes",
        "FluxosAprovacao", "Integracoes", "Usuarios", "Empresas", "Equipes",
        "Departamentos", "Cargos", "Etiquetas", "Moedas", "Localidades"
    }
})).AllowAnonymous();

app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<SegurancaHeadersMiddleware>();
app.UseMiddleware<TratamentoErrosMiddleware>();
app.UseMiddleware<SanitizacaoMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapControllers();

// Fallback: retornar 404 JSON para rotas não mapeadas (evita exposição de info)
app.MapFallback(() => Results.Json(new { erro = "Rota não encontrada.", status = 404 }, statusCode: 404));

// Seed de dados iniciais + migrations automáticas em dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await AST.Comercial.Infrastructure.Data.SeedDados.ExecutarAsync(db);
}

app.Run();

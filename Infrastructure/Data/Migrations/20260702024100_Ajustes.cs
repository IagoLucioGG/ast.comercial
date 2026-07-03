using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Ajustes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "ValorCampo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Usuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "StatusNegocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "StatusCliente",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "SecaoProposta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "RegraCampo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "RamoAtividade",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Proposta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Produto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "PorteEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "PessoaContato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "ParcelaProposta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "OrigemContato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "OpcaoCampo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Negocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "MotivoPerda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Moeda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "MembroEquipe",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "ItemProposta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "GrupoProduto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Funil",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "FiltroAutomacao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "FamiliaProduto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "EtiquetaRegistro",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Etiqueta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "EtapaFunil",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Equipe",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Departamento",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "ConfiguracaoCampo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Cliente",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Cargo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Campo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Automacao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "Atividade",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveExterna",
                table: "AcaoAutomacao",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FluxoAprovacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FluxoAprovacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formulario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formulario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ApiUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TipoAuth = table.Column<string>(type: "text", nullable: false),
                    ApiToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AuthUsuario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthSenha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OAuthTokenUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OAuthScope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApiKeyHeader = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKeyValor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HeadersFixos = table.Column<string>(type: "jsonb", nullable: true),
                    TipoApi = table.Column<string>(type: "text", nullable: false),
                    WebhookSecret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChaveWebhook = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Configuracao = table.Column<string>(type: "jsonb", nullable: true),
                    UltimaSincronizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integracao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiltroFluxoAprovacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FluxoAprovacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Grupo = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CampoReferencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operador = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiltroFluxoAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiltroFluxoAprovacao_FluxoAprovacao_FluxoAprovacaoId",
                        column: x => x.FluxoAprovacaoId,
                        principalTable: "FluxoAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NivelAprovacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FluxoAprovacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NivelAprovacao_FluxoAprovacao_FluxoAprovacaoId",
                        column: x => x.FluxoAprovacaoId,
                        principalTable: "FluxoAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoAprovacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FluxoAprovacaoId = table.Column<long>(type: "bigint", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    NivelAtual = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AprovadoPorId = table.Column<long>(type: "bigint", nullable: true),
                    AprovadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejeitadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoAprovacao_FluxoAprovacao_FluxoAprovacaoId",
                        column: x => x.FluxoAprovacaoId,
                        principalTable: "FluxoAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoAprovacao_Usuario_AprovadoPorId",
                        column: x => x.AprovadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SecaoFormulario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Colunas = table.Column<int>(type: "integer", nullable: true),
                    Colapsavel = table.Column<bool>(type: "boolean", nullable: false),
                    IniciaColapsada = table.Column<bool>(type: "boolean", nullable: false),
                    FormularioId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecaoFormulario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecaoFormulario_Formulario_FormularioId",
                        column: x => x.FormularioId,
                        principalTable: "Formulario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FluxoIntegracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IntegracaoId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direcao = table.Column<string>(type: "text", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    ModoEntrada = table.Column<string>(type: "text", nullable: true),
                    EventoGatilho = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EndpointSaida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetodoHttpSaida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TemplateBody = table.Column<string>(type: "text", nullable: true),
                    WebhookCampoIdentificador = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebhookValorIdentificador = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EndpointPolling = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetodoHttpPolling = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IntervaloPollingMinutos = table.Column<int>(type: "integer", nullable: true),
                    TamanhoPagina = table.Column<int>(type: "integer", nullable: true),
                    CampoListaResponse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CampoPaginacaoResponse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UltimaSincronizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duplicidade = table.Column<string>(type: "text", nullable: false),
                    CampoChaveDuplicidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Configuracao = table.Column<string>(type: "jsonb", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FluxoIntegracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FluxoIntegracao_Integracao_IntegracaoId",
                        column: x => x.IntegracaoId,
                        principalTable: "Integracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AprovadorNivel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NivelAprovacaoId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Perfil = table.Column<string>(type: "text", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AprovadorNivel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AprovadorNivel_NivelAprovacao_NivelAprovacaoId",
                        column: x => x.NivelAprovacaoId,
                        principalTable: "NivelAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AprovadorNivel_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CampoFormulario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SecaoFormularioId = table.Column<long>(type: "bigint", nullable: true),
                    FormularioId = table.Column<long>(type: "bigint", nullable: false),
                    NomeCampoFixo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CampoId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Coluna = table.Column<int>(type: "integer", nullable: true),
                    LarguraColunas = table.Column<int>(type: "integer", nullable: true),
                    Rotulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Dica = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Visivel = table.Column<bool>(type: "boolean", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    SomenteLeitura = table.Column<bool>(type: "boolean", nullable: false),
                    FormulaVisibilidade = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaObrigatoriedade = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaCalculo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaValorPadrao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaSomenteLeitura = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampoFormulario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampoFormulario_Campo_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CampoFormulario_Formulario_FormularioId",
                        column: x => x.FormularioId,
                        principalTable: "Formulario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampoFormulario_SecaoFormulario_SecaoFormularioId",
                        column: x => x.SecaoFormularioId,
                        principalTable: "SecaoFormulario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EtapaFluxoIntegracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FluxoIntegracaoId = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MetodoHttp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CampoChaveConsulta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CampoInternoChave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ArmazenarResultadoComo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CampoResultadoResponse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CondicaoCampo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CondicaoOperador = table.Column<string>(type: "text", nullable: true),
                    CondicaoValor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PararSeErro = table.Column<bool>(type: "boolean", nullable: false),
                    TemplateBody = table.Column<string>(type: "text", nullable: true),
                    Configuracao = table.Column<string>(type: "jsonb", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapaFluxoIntegracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapaFluxoIntegracao_FluxoIntegracao_FluxoIntegracaoId",
                        column: x => x.FluxoIntegracaoId,
                        principalTable: "FluxoIntegracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogIntegracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IntegracaoId = table.Column<long>(type: "bigint", nullable: false),
                    FluxoIntegracaoId = table.Column<long>(type: "bigint", nullable: true),
                    Direcao = table.Column<string>(type: "text", nullable: false),
                    Sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetodoHttp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    Erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DuracaoMs = table.Column<int>(type: "integer", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: true),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogIntegracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogIntegracao_FluxoIntegracao_FluxoIntegracaoId",
                        column: x => x.FluxoIntegracaoId,
                        principalTable: "FluxoIntegracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LogIntegracao_Integracao_IntegracaoId",
                        column: x => x.IntegracaoId,
                        principalTable: "Integracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapeamentoCampoIntegracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IntegracaoId = table.Column<long>(type: "bigint", nullable: false),
                    FluxoIntegracaoId = table.Column<long>(type: "bigint", nullable: true),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    CampoInterno = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CampoExterno = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direcao = table.Column<string>(type: "text", nullable: false),
                    Transformacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValorPadrao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapeamentoCampoIntegracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapeamentoCampoIntegracao_FluxoIntegracao_FluxoIntegracaoId",
                        column: x => x.FluxoIntegracaoId,
                        principalTable: "FluxoIntegracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapeamentoCampoIntegracao_Integracao_IntegracaoId",
                        column: x => x.IntegracaoId,
                        principalTable: "Integracao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AprovadorNivel_NivelAprovacaoId",
                table: "AprovadorNivel",
                column: "NivelAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_AprovadorNivel_UsuarioId",
                table: "AprovadorNivel",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CampoFormulario_CampoId",
                table: "CampoFormulario",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampoFormulario_FormularioId_Ordem",
                table: "CampoFormulario",
                columns: new[] { "FormularioId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_CampoFormulario_SecaoFormularioId",
                table: "CampoFormulario",
                column: "SecaoFormularioId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapaFluxoIntegracao_FluxoIntegracaoId_Ordem",
                table: "EtapaFluxoIntegracao",
                columns: new[] { "FluxoIntegracaoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_FiltroFluxoAprovacao_FluxoAprovacaoId",
                table: "FiltroFluxoAprovacao",
                column: "FluxoAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_FluxoAprovacao_EmpresaId_EntidadeAlvo_Ordem",
                table: "FluxoAprovacao",
                columns: new[] { "EmpresaId", "EntidadeAlvo", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_FluxoIntegracao_IntegracaoId_Direcao",
                table: "FluxoIntegracao",
                columns: new[] { "IntegracaoId", "Direcao" });

            migrationBuilder.CreateIndex(
                name: "IX_Formulario_EmpresaId_EntidadeAlvo_Nome",
                table: "Formulario",
                columns: new[] { "EmpresaId", "EntidadeAlvo", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integracao_ChaveWebhook",
                table: "Integracao",
                column: "ChaveWebhook",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integracao_EmpresaId_Provedor",
                table: "Integracao",
                columns: new[] { "EmpresaId", "Provedor" });

            migrationBuilder.CreateIndex(
                name: "IX_LogIntegracao_FluxoIntegracaoId",
                table: "LogIntegracao",
                column: "FluxoIntegracaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LogIntegracao_IntegracaoId_CriadoEm",
                table: "LogIntegracao",
                columns: new[] { "IntegracaoId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LogIntegracao_Sucesso",
                table: "LogIntegracao",
                column: "Sucesso");

            migrationBuilder.CreateIndex(
                name: "IX_MapeamentoCampoIntegracao_FluxoIntegracaoId",
                table: "MapeamentoCampoIntegracao",
                column: "FluxoIntegracaoId");

            migrationBuilder.CreateIndex(
                name: "IX_MapeamentoCampoIntegracao_IntegracaoId_FluxoIntegracaoId_Or~",
                table: "MapeamentoCampoIntegracao",
                columns: new[] { "IntegracaoId", "FluxoIntegracaoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_NivelAprovacao_FluxoAprovacaoId_Nivel",
                table: "NivelAprovacao",
                columns: new[] { "FluxoAprovacaoId", "Nivel" });

            migrationBuilder.CreateIndex(
                name: "IX_SecaoFormulario_FormularioId_Ordem",
                table: "SecaoFormulario",
                columns: new[] { "FormularioId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAprovacao_AprovadoPorId",
                table: "SolicitacaoAprovacao",
                column: "AprovadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAprovacao_EntidadeAlvo_RegistroId_Status",
                table: "SolicitacaoAprovacao",
                columns: new[] { "EntidadeAlvo", "RegistroId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAprovacao_FluxoAprovacaoId",
                table: "SolicitacaoAprovacao",
                column: "FluxoAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAprovacao_Status",
                table: "SolicitacaoAprovacao",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AprovadorNivel");

            migrationBuilder.DropTable(
                name: "CampoFormulario");

            migrationBuilder.DropTable(
                name: "EtapaFluxoIntegracao");

            migrationBuilder.DropTable(
                name: "FiltroFluxoAprovacao");

            migrationBuilder.DropTable(
                name: "LogIntegracao");

            migrationBuilder.DropTable(
                name: "MapeamentoCampoIntegracao");

            migrationBuilder.DropTable(
                name: "SolicitacaoAprovacao");

            migrationBuilder.DropTable(
                name: "NivelAprovacao");

            migrationBuilder.DropTable(
                name: "SecaoFormulario");

            migrationBuilder.DropTable(
                name: "FluxoIntegracao");

            migrationBuilder.DropTable(
                name: "FluxoAprovacao");

            migrationBuilder.DropTable(
                name: "Formulario");

            migrationBuilder.DropTable(
                name: "Integracao");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "ValorCampo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "StatusNegocio");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "StatusCliente");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "SecaoProposta");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "RegraCampo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "RamoAtividade");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "PorteEmpresa");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "PessoaContato");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "ParcelaProposta");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "OrigemContato");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "OpcaoCampo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "MotivoPerda");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Moeda");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "MembroEquipe");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "ItemProposta");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "GrupoProduto");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Funil");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "FiltroAutomacao");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "FamiliaProduto");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "EtiquetaRegistro");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Etiqueta");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "EtapaFunil");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Equipe");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Departamento");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "ConfiguracaoCampo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Cargo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Campo");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Automacao");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "Atividade");

            migrationBuilder.DropColumn(
                name: "ChaveExterna",
                table: "AcaoAutomacao");
        }
    }
}

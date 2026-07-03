using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicializacaoBancoDeDados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Automacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Gatilho = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    ImpedirCascata = table.Column<bool>(type: "boolean", nullable: false),
                    GatilhoCampoReferencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CronExpressao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HorarioExecucao = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ProximaExecucao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaExecucao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FormulaCondicao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cargo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departamento",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DepartamentoPaiId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departamento_Departamento_DepartamentoPaiId",
                        column: x => x.DepartamentoPaiId,
                        principalTable: "Departamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Empresa",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Cnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipe",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etiqueta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etiqueta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamiliaProduto",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamiliaProduto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funil",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Localidade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    LocalidadePaiId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Localidade_Localidade_LocalidadePaiId",
                        column: x => x.LocalidadePaiId,
                        principalTable: "Localidade",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Moeda",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Simbolo = table.Column<string>(type: "text", nullable: true),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moeda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MotivoPerda",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivoPerda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrigemContato",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrigemContato", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PorteEmpresa",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PorteEmpresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RamoAtividade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RamoAtividade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistroAlteracao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Usuario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Acao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DadosAntes = table.Column<string>(type: "jsonb", nullable: true),
                    DadosDepois = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroAlteracao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusCliente",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusCliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusNegocio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    TipoBase = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusNegocio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcaoAutomacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutomacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Configuracao = table.Column<string>(type: "jsonb", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcaoAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcaoAutomacao_Automacao_AutomacaoId",
                        column: x => x.AutomacaoId,
                        principalTable: "Automacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecucaoAutomacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    AutomacaoId = table.Column<long>(type: "bigint", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: true),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DuracaoMs = table.Column<int>(type: "integer", nullable: false),
                    Detalhes = table.Column<string>(type: "jsonb", nullable: true),
                    Erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DisparadoPor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucaoAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecucaoAutomacao_Automacao_AutomacaoId",
                        column: x => x.AutomacaoId,
                        principalTable: "Automacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilaAutomacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    AutomacaoId = table.Column<long>(type: "bigint", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: true),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Gatilho = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    MaxTentativas = table.Column<int>(type: "integer", nullable: false),
                    ProximaTentativa = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimoErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DadosEvento = table.Column<string>(type: "jsonb", nullable: true),
                    DisparadoPor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AutomacaoOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilaAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilaAutomacao_Automacao_AutomacaoId",
                        column: x => x.AutomacaoId,
                        principalTable: "Automacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiltroAutomacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutomacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Grupo = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CampoReferencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operador = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValorAnterior = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiltroAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiltroAutomacao_Automacao_AutomacaoId",
                        column: x => x.AutomacaoId,
                        principalTable: "Automacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<string>(type: "text", nullable: false),
                    EmailConfirmado = table.Column<bool>(type: "boolean", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenIntegracao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescricaoIntegracao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CargoId = table.Column<long>(type: "bigint", nullable: true),
                    DepartamentoId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Cargo_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Usuario_Departamento_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Usuario_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EtiquetaRegistro",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EtiquetaId = table.Column<long>(type: "bigint", nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtiquetaRegistro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtiquetaRegistro_Etiqueta_EtiquetaId",
                        column: x => x.EtiquetaId,
                        principalTable: "Etiqueta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrupoProduto",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    FamiliaProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoProduto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrupoProduto_FamiliaProduto_FamiliaProdutoId",
                        column: x => x.FamiliaProdutoId,
                        principalTable: "FamiliaProduto",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EtapaFunil",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    DiasParaExpirar = table.Column<int>(type: "integer", nullable: true),
                    FunilId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapaFunil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapaFunil_Funil_FunilId",
                        column: x => x.FunilId,
                        principalTable: "Funil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Site = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    StatusClienteId = table.Column<long>(type: "bigint", nullable: true),
                    OrigemId = table.Column<long>(type: "bigint", nullable: true),
                    RamoAtividadeId = table.Column<long>(type: "bigint", nullable: true),
                    PorteEmpresaId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cliente_OrigemContato_OrigemId",
                        column: x => x.OrigemId,
                        principalTable: "OrigemContato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cliente_PorteEmpresa_PorteEmpresaId",
                        column: x => x.PorteEmpresaId,
                        principalTable: "PorteEmpresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cliente_RamoAtividade_RamoAtividadeId",
                        column: x => x.RamoAtividadeId,
                        principalTable: "RamoAtividade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cliente_StatusCliente_StatusClienteId",
                        column: x => x.StatusClienteId,
                        principalTable: "StatusCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MembroEquipe",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipeId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Papel = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembroEquipe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembroEquipe_Equipe_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembroEquipe_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenAcesso",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmitidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revogado = table.Column<bool>(type: "boolean", nullable: false),
                    RevogadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Origem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenAcesso_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Produto",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoCusto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Unidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Disponivel = table.Column<bool>(type: "boolean", nullable: false),
                    FamiliaProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    GrupoProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produto_FamiliaProduto_FamiliaProdutoId",
                        column: x => x.FamiliaProdutoId,
                        principalTable: "FamiliaProduto",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Produto_GrupoProduto_GrupoProdutoId",
                        column: x => x.GrupoProdutoId,
                        principalTable: "GrupoProduto",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Campo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Chave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Visivel = table.Column<bool>(type: "boolean", nullable: false),
                    SomenteLeitura = table.Column<bool>(type: "boolean", nullable: false),
                    ValorPadrao = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    Formula = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TamanhoMinimo = table.Column<int>(type: "integer", nullable: true),
                    TamanhoMaximo = table.Column<int>(type: "integer", nullable: true),
                    ValorMinimo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Mascara = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EtapaFunilId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campo_EtapaFunil_EtapaFunilId",
                        column: x => x.EtapaFunilId,
                        principalTable: "EtapaFunil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PessoaContato",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Decisor = table.Column<bool>(type: "boolean", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PessoaContato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PessoaContato_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracaoCampo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    NomeCampoFixo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CampoId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Rotulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Dica = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Agrupamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Largura = table.Column<int>(type: "integer", nullable: true),
                    FormulaVisibilidade = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaObrigatoriedade = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaCalculo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaValorPadrao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormulaSomenteLeitura = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Visivel = table.Column<bool>(type: "boolean", nullable: false),
                    SomenteLeitura = table.Column<bool>(type: "boolean", nullable: false),
                    FunilId = table.Column<long>(type: "bigint", nullable: true),
                    EtapaFunilId = table.Column<long>(type: "bigint", nullable: true),
                    PropostaTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracaoCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracaoCampo_Campo_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfiguracaoCampo_EtapaFunil_EtapaFunilId",
                        column: x => x.EtapaFunilId,
                        principalTable: "EtapaFunil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConfiguracaoCampo_Funil_FunilId",
                        column: x => x.FunilId,
                        principalTable: "Funil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OpcaoCampo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Valor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rotulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CampoId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcaoCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcaoCampo_Campo_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegraCampo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampoId = table.Column<long>(type: "bigint", nullable: false),
                    CampoOrigemId = table.Column<long>(type: "bigint", nullable: false),
                    Operador = table.Column<string>(type: "text", nullable: false),
                    ValorComparacao = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    Acao = table.Column<string>(type: "text", nullable: false),
                    FormulaCondicao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegraCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegraCampo_Campo_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegraCampo_Campo_CampoOrigemId",
                        column: x => x.CampoOrigemId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FechadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrevisaoFechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    PessoaContatoId = table.Column<long>(type: "bigint", nullable: true),
                    FunilId = table.Column<long>(type: "bigint", nullable: false),
                    EtapaId = table.Column<long>(type: "bigint", nullable: false),
                    StatusId = table.Column<long>(type: "bigint", nullable: true),
                    MotivoPerdaId = table.Column<long>(type: "bigint", nullable: true),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Negocio_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Negocio_EtapaFunil_EtapaId",
                        column: x => x.EtapaId,
                        principalTable: "EtapaFunil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Negocio_Funil_FunilId",
                        column: x => x.FunilId,
                        principalTable: "Funil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Negocio_MotivoPerda_MotivoPerdaId",
                        column: x => x.MotivoPerdaId,
                        principalTable: "MotivoPerda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Negocio_PessoaContato_PessoaContatoId",
                        column: x => x.PessoaContatoId,
                        principalTable: "PessoaContato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Negocio_StatusNegocio_StatusId",
                        column: x => x.StatusId,
                        principalTable: "StatusNegocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Atividade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcluidaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Concluida = table.Column<bool>(type: "boolean", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    PessoaContatoId = table.Column<long>(type: "bigint", nullable: true),
                    NegocioId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atividade_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Atividade_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Atividade_PessoaContato_PessoaContatoId",
                        column: x => x.PessoaContatoId,
                        principalTable: "PessoaContato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Proposta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Numero = table.Column<int>(type: "integer", nullable: true),
                    Revisao = table.Column<int>(type: "integer", nullable: true),
                    UltimaRevisao = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaRevisaoId = table.Column<long>(type: "bigint", nullable: true),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CustoFrete = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ModalFrete = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumeroParcelas = table.Column<int>(type: "integer", nullable: true),
                    PrazoEntrega = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FormaPagamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    StatusAprovacao = table.Column<string>(type: "text", nullable: true),
                    AprovadorId = table.Column<long>(type: "bigint", nullable: true),
                    CodigoFonteHeader = table.Column<string>(type: "text", nullable: true),
                    AlturaHeader = table.Column<int>(type: "integer", nullable: true),
                    CodigoFonteFooter = table.Column<string>(type: "text", nullable: true),
                    AlturaFooter = table.Column<int>(type: "integer", nullable: true),
                    CodigoFonteBody = table.Column<string>(type: "text", nullable: true),
                    CodigoFontePreview = table.Column<string>(type: "text", nullable: true),
                    CodigoFonteCapa = table.Column<string>(type: "text", nullable: true),
                    TemCapa = table.Column<bool>(type: "boolean", nullable: false),
                    TemPaginacao = table.Column<bool>(type: "boolean", nullable: false),
                    MargemSuperior = table.Column<int>(type: "integer", nullable: true),
                    MargemInferior = table.Column<int>(type: "integer", nullable: true),
                    MargemLateral = table.Column<int>(type: "integer", nullable: true),
                    NomeArquivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UrlDocumento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Chave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Compartilhada = table.Column<bool>(type: "boolean", nullable: false),
                    DataCompartilhamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AceitaExternamente = table.Column<bool>(type: "boolean", nullable: false),
                    NotificacoesExternas = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaAberturaExterna = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    NegocioId = table.Column<long>(type: "bigint", nullable: true),
                    ResponsavelId = table.Column<long>(type: "bigint", nullable: true),
                    TemplateId = table.Column<long>(type: "bigint", nullable: true),
                    CriadorId = table.Column<long>(type: "bigint", nullable: true),
                    AtualizadorId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposta_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Proposta_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ParcelaProposta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PropostaId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcelaProposta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcelaProposta_Proposta_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Proposta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecaoProposta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PropostaId = table.Column<long>(type: "bigint", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecaoProposta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecaoProposta_Proposta_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Proposta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemProposta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropostaId = table.Column<long>(type: "bigint", nullable: false),
                    SecaoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProdutoCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantidade = table.Column<double>(type: "double precision", nullable: true),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Desconto = table.Column<double>(type: "double precision", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemProposta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemProposta_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemProposta_Proposta_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Proposta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemProposta_SecaoProposta_SecaoId",
                        column: x => x.SecaoId,
                        principalTable: "SecaoProposta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ValorCampo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampoId = table.Column<long>(type: "bigint", nullable: false),
                    CampoChave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    ValorTexto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValorTextoGrande = table.Column<string>(type: "text", nullable: true),
                    ValorInteiro = table.Column<int>(type: "integer", nullable: true),
                    ValorDecimal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorDataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorBooleano = table.Column<bool>(type: "boolean", nullable: true),
                    OpcaoValorId = table.Column<long>(type: "bigint", nullable: true),
                    OpcaoValorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UsuarioValorId = table.Column<long>(type: "bigint", nullable: true),
                    UsuarioValorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UsuarioValorAvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProdutoValorId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoValorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClienteValorId = table.Column<long>(type: "bigint", nullable: true),
                    ClienteValorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClienteValorDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MoedaValorId = table.Column<long>(type: "bigint", nullable: true),
                    AnexoValorId = table.Column<long>(type: "bigint", nullable: true),
                    AnexoValorNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AtividadeId = table.Column<long>(type: "bigint", nullable: true),
                    CargoId = table.Column<long>(type: "bigint", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    DepartamentoId = table.Column<long>(type: "bigint", nullable: true),
                    EquipeId = table.Column<long>(type: "bigint", nullable: true),
                    EtapaFunilId = table.Column<long>(type: "bigint", nullable: true),
                    FunilId = table.Column<long>(type: "bigint", nullable: true),
                    ItemPropostaId = table.Column<long>(type: "bigint", nullable: true),
                    NegocioId = table.Column<long>(type: "bigint", nullable: true),
                    ParcelaPropostaId = table.Column<long>(type: "bigint", nullable: true),
                    PessoaContatoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    PropostaId = table.Column<long>(type: "bigint", nullable: true),
                    SecaoPropostaId = table.Column<long>(type: "bigint", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValorCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValorCampo_Atividade_AtividadeId",
                        column: x => x.AtividadeId,
                        principalTable: "Atividade",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Campo_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValorCampo_Cargo_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Departamento_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValorCampo_Equipe_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipe",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_EtapaFunil_EtapaFunilId",
                        column: x => x.EtapaFunilId,
                        principalTable: "EtapaFunil",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Funil_FunilId",
                        column: x => x.FunilId,
                        principalTable: "Funil",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_ItemProposta_ItemPropostaId",
                        column: x => x.ItemPropostaId,
                        principalTable: "ItemProposta",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_ParcelaProposta_ParcelaPropostaId",
                        column: x => x.ParcelaPropostaId,
                        principalTable: "ParcelaProposta",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_PessoaContato_PessoaContatoId",
                        column: x => x.PessoaContatoId,
                        principalTable: "PessoaContato",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Proposta_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Proposta",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_SecaoProposta_SecaoPropostaId",
                        column: x => x.SecaoPropostaId,
                        principalTable: "SecaoProposta",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValorCampo_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcaoAutomacao_AutomacaoId_Ordem",
                table: "AcaoAutomacao",
                columns: new[] { "AutomacaoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_ClienteId",
                table: "Atividade",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_Concluida",
                table: "Atividade",
                column: "Concluida");

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_DataVencimento",
                table: "Atividade",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_NegocioId",
                table: "Atividade",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_PessoaContatoId",
                table: "Atividade",
                column: "PessoaContatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Automacao_EmpresaId_EntidadeAlvo_Gatilho",
                table: "Automacao",
                columns: new[] { "EmpresaId", "EntidadeAlvo", "Gatilho" });

            migrationBuilder.CreateIndex(
                name: "IX_Automacao_ProximaExecucao",
                table: "Automacao",
                column: "ProximaExecucao");

            migrationBuilder.CreateIndex(
                name: "IX_Campo_EntidadeAlvo",
                table: "Campo",
                column: "EntidadeAlvo");

            migrationBuilder.CreateIndex(
                name: "IX_Campo_EntidadeAlvo_Chave",
                table: "Campo",
                columns: new[] { "EntidadeAlvo", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campo_EtapaFunilId",
                table: "Campo",
                column: "EtapaFunilId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_EmpresaId_Nome",
                table: "Cargo",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Documento",
                table: "Cliente",
                column: "Documento");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Email",
                table: "Cliente",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Nome",
                table: "Cliente",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_OrigemId",
                table: "Cliente",
                column: "OrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_PorteEmpresaId",
                table: "Cliente",
                column: "PorteEmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_RamoAtividadeId",
                table: "Cliente",
                column: "RamoAtividadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_StatusClienteId",
                table: "Cliente",
                column: "StatusClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoCampo_CampoId",
                table: "ConfiguracaoCampo",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoCampo_EmpresaId_EntidadeAlvo_CampoId_FunilId_Et~",
                table: "ConfiguracaoCampo",
                columns: new[] { "EmpresaId", "EntidadeAlvo", "CampoId", "FunilId", "EtapaFunilId", "PropostaTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoCampo_EntidadeAlvo_NomeCampoFixo",
                table: "ConfiguracaoCampo",
                columns: new[] { "EntidadeAlvo", "NomeCampoFixo" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoCampo_EtapaFunilId",
                table: "ConfiguracaoCampo",
                column: "EtapaFunilId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoCampo_FunilId",
                table: "ConfiguracaoCampo",
                column: "FunilId");

            migrationBuilder.CreateIndex(
                name: "IX_Departamento_DepartamentoPaiId",
                table: "Departamento",
                column: "DepartamentoPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Departamento_EmpresaId_Nome",
                table: "Departamento",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_Cnpj",
                table: "Empresa",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipe_EmpresaId_Nome",
                table: "Equipe",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapaFunil_FunilId_Ordem",
                table: "EtapaFunil",
                columns: new[] { "FunilId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_EtiquetaRegistro_EtiquetaId",
                table: "EtiquetaRegistro",
                column: "EtiquetaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoAutomacao_AutomacaoId",
                table: "ExecucaoAutomacao",
                column: "AutomacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoAutomacao_EmpresaId_AutomacaoId_IniciadaEm",
                table: "ExecucaoAutomacao",
                columns: new[] { "EmpresaId", "AutomacaoId", "IniciadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoAutomacao_EntidadeAlvo_RegistroId",
                table: "ExecucaoAutomacao",
                columns: new[] { "EntidadeAlvo", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoAutomacao_IniciadaEm",
                table: "ExecucaoAutomacao",
                column: "IniciadaEm");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoAutomacao_Status",
                table: "ExecucaoAutomacao",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FilaAutomacao_AutomacaoId_Status",
                table: "FilaAutomacao",
                columns: new[] { "AutomacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FilaAutomacao_EmpresaId_Status",
                table: "FilaAutomacao",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FilaAutomacao_EntidadeAlvo_RegistroId",
                table: "FilaAutomacao",
                columns: new[] { "EntidadeAlvo", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_FilaAutomacao_Status_ProximaTentativa_CriadoEm",
                table: "FilaAutomacao",
                columns: new[] { "Status", "ProximaTentativa", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_FiltroAutomacao_AutomacaoId_Grupo_Ordem",
                table: "FiltroAutomacao",
                columns: new[] { "AutomacaoId", "Grupo", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_GrupoProduto_FamiliaProdutoId",
                table: "GrupoProduto",
                column: "FamiliaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProposta_ProdutoId",
                table: "ItemProposta",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemProposta_PropostaId_Ordem",
                table: "ItemProposta",
                columns: new[] { "PropostaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProposta_SecaoId",
                table: "ItemProposta",
                column: "SecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Localidade_LocalidadePaiId",
                table: "Localidade",
                column: "LocalidadePaiId");

            migrationBuilder.CreateIndex(
                name: "IX_MembroEquipe_EquipeId_UsuarioId",
                table: "MembroEquipe",
                columns: new[] { "EquipeId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembroEquipe_UsuarioId",
                table: "MembroEquipe",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_ClienteId",
                table: "Negocio",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_EtapaId",
                table: "Negocio",
                column: "EtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_FunilId",
                table: "Negocio",
                column: "FunilId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_MotivoPerdaId",
                table: "Negocio",
                column: "MotivoPerdaId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_PessoaContatoId",
                table: "Negocio",
                column: "PessoaContatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_StatusId",
                table: "Negocio",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OpcaoCampo_CampoId_Ordem",
                table: "OpcaoCampo",
                columns: new[] { "CampoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ParcelaProposta_PropostaId_Numero",
                table: "ParcelaProposta",
                columns: new[] { "PropostaId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_PessoaContato_ClienteId",
                table: "PessoaContato",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PessoaContato_Email",
                table: "PessoaContato",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PessoaContato_Nome",
                table: "PessoaContato",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Produto_Codigo",
                table: "Produto",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_Produto_FamiliaProdutoId",
                table: "Produto",
                column: "FamiliaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produto_GrupoProdutoId",
                table: "Produto",
                column: "GrupoProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produto_Nome",
                table: "Produto",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_Chave",
                table: "Proposta",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_ClienteId",
                table: "Proposta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_EmpresaId_Numero",
                table: "Proposta",
                columns: new[] { "EmpresaId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_IsTemplate",
                table: "Proposta",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_NegocioId",
                table: "Proposta",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposta_StatusAprovacao",
                table: "Proposta",
                column: "StatusAprovacao");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAlteracao_DataHora",
                table: "RegistroAlteracao",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAlteracao_EntidadeAlvo_RegistroId",
                table: "RegistroAlteracao",
                columns: new[] { "EntidadeAlvo", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAlteracao_Usuario",
                table: "RegistroAlteracao",
                column: "Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_RegraCampo_CampoId",
                table: "RegraCampo",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegraCampo_CampoOrigemId",
                table: "RegraCampo",
                column: "CampoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_SecaoProposta_PropostaId_Ordem",
                table: "SecaoProposta",
                columns: new[] { "PropostaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenAcesso_RefreshToken",
                table: "TokenAcesso",
                column: "RefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_TokenAcesso_Token",
                table: "TokenAcesso",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_TokenAcesso_UsuarioId_Revogado",
                table: "TokenAcesso",
                columns: new[] { "UsuarioId", "Revogado" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_CargoId",
                table: "Usuario",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_DepartamentoId",
                table: "Usuario",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_EmpresaId",
                table: "Usuario",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_TokenIntegracao",
                table: "Usuario",
                column: "TokenIntegracao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_AtividadeId",
                table: "ValorCampo",
                column: "AtividadeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_CampoChave",
                table: "ValorCampo",
                column: "CampoChave");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_CampoId_RegistroId_EntidadeAlvo",
                table: "ValorCampo",
                columns: new[] { "CampoId", "RegistroId", "EntidadeAlvo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_CargoId",
                table: "ValorCampo",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ClienteId",
                table: "ValorCampo",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_DepartamentoId",
                table: "ValorCampo",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_EmpresaId",
                table: "ValorCampo",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_EquipeId",
                table: "ValorCampo",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_EtapaFunilId",
                table: "ValorCampo",
                column: "EtapaFunilId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_FunilId",
                table: "ValorCampo",
                column: "FunilId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ItemPropostaId",
                table: "ValorCampo",
                column: "ItemPropostaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_NegocioId",
                table: "ValorCampo",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ParcelaPropostaId",
                table: "ValorCampo",
                column: "ParcelaPropostaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_PessoaContatoId",
                table: "ValorCampo",
                column: "PessoaContatoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ProdutoId",
                table: "ValorCampo",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_PropostaId",
                table: "ValorCampo",
                column: "PropostaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_RegistroId_EntidadeAlvo",
                table: "ValorCampo",
                columns: new[] { "RegistroId", "EntidadeAlvo" });

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_SecaoPropostaId",
                table: "ValorCampo",
                column: "SecaoPropostaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_UsuarioId",
                table: "ValorCampo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ValorDataHora",
                table: "ValorCampo",
                column: "ValorDataHora");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ValorInteiro",
                table: "ValorCampo",
                column: "ValorInteiro");

            migrationBuilder.CreateIndex(
                name: "IX_ValorCampo_ValorTexto",
                table: "ValorCampo",
                column: "ValorTexto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcaoAutomacao");

            migrationBuilder.DropTable(
                name: "ConfiguracaoCampo");

            migrationBuilder.DropTable(
                name: "EtiquetaRegistro");

            migrationBuilder.DropTable(
                name: "ExecucaoAutomacao");

            migrationBuilder.DropTable(
                name: "FilaAutomacao");

            migrationBuilder.DropTable(
                name: "FiltroAutomacao");

            migrationBuilder.DropTable(
                name: "Localidade");

            migrationBuilder.DropTable(
                name: "MembroEquipe");

            migrationBuilder.DropTable(
                name: "Moeda");

            migrationBuilder.DropTable(
                name: "OpcaoCampo");

            migrationBuilder.DropTable(
                name: "RegistroAlteracao");

            migrationBuilder.DropTable(
                name: "RegraCampo");

            migrationBuilder.DropTable(
                name: "TokenAcesso");

            migrationBuilder.DropTable(
                name: "ValorCampo");

            migrationBuilder.DropTable(
                name: "Etiqueta");

            migrationBuilder.DropTable(
                name: "Automacao");

            migrationBuilder.DropTable(
                name: "Atividade");

            migrationBuilder.DropTable(
                name: "Campo");

            migrationBuilder.DropTable(
                name: "Equipe");

            migrationBuilder.DropTable(
                name: "ItemProposta");

            migrationBuilder.DropTable(
                name: "ParcelaProposta");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Produto");

            migrationBuilder.DropTable(
                name: "SecaoProposta");

            migrationBuilder.DropTable(
                name: "Cargo");

            migrationBuilder.DropTable(
                name: "Departamento");

            migrationBuilder.DropTable(
                name: "Empresa");

            migrationBuilder.DropTable(
                name: "GrupoProduto");

            migrationBuilder.DropTable(
                name: "Proposta");

            migrationBuilder.DropTable(
                name: "FamiliaProduto");

            migrationBuilder.DropTable(
                name: "Negocio");

            migrationBuilder.DropTable(
                name: "EtapaFunil");

            migrationBuilder.DropTable(
                name: "MotivoPerda");

            migrationBuilder.DropTable(
                name: "PessoaContato");

            migrationBuilder.DropTable(
                name: "StatusNegocio");

            migrationBuilder.DropTable(
                name: "Funil");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropTable(
                name: "OrigemContato");

            migrationBuilder.DropTable(
                name: "PorteEmpresa");

            migrationBuilder.DropTable(
                name: "RamoAtividade");

            migrationBuilder.DropTable(
                name: "StatusCliente");
        }
    }
}

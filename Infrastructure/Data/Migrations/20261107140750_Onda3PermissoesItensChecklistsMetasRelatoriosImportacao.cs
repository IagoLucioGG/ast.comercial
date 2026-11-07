using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Onda3PermissoesItensChecklistsMetasRelatoriosImportacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComentarioDecisao",
                table: "SolicitacaoAprovacao",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SolicitadoPorId",
                table: "SolicitacaoAprovacao",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChecklistEtapa",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EtapaFunilId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistEtapa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistEtapa_EtapaFunil_EtapaFunilId",
                        column: x => x.EtapaFunilId,
                        principalTable: "EtapaFunil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Importacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalLinhas = table.Column<int>(type: "integer", nullable: false),
                    Sucesso = table.Column<int>(type: "integer", nullable: false),
                    Falhas = table.Column<int>(type: "integer", nullable: false),
                    MapeamentoJson = table.Column<string>(type: "text", nullable: false),
                    ErrosJson = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    ConcluidaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Importacao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemNegocio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NegocioId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemNegocio_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemNegocio_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListaPreco",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VigenciaFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaPreco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaPreco_Moeda_MoedaId",
                        column: x => x.MoedaId,
                        principalTable: "Moeda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Metrica = table.Column<string>(type: "text", nullable: false),
                    Alvo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    EquipeId = table.Column<long>(type: "bigint", nullable: true),
                    FunilId = table.Column<long>(type: "bigint", nullable: true),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meta_Equipe_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meta_Funil_FunilId",
                        column: x => x.FunilId,
                        principalTable: "Funil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meta_Moeda_MoedaId",
                        column: x => x.MoedaId,
                        principalTable: "Moeda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meta_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PainelRelatorio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Compartilhado = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoPorUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PainelRelatorio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PainelRelatorio_Usuario_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissaoPerfil",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Perfil = table.Column<string>(type: "text", nullable: false),
                    Entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PodeVer = table.Column<bool>(type: "boolean", nullable: false),
                    PodeCriar = table.Column<bool>(type: "boolean", nullable: false),
                    PodeEditar = table.Column<bool>(type: "boolean", nullable: false),
                    PodeExcluir = table.Column<bool>(type: "boolean", nullable: false),
                    Escopo = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissaoPerfil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemChecklistEtapa",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChecklistEtapaId = table.Column<long>(type: "bigint", nullable: false),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_ItemChecklistEtapa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemChecklistEtapa_ChecklistEtapa_ChecklistEtapaId",
                        column: x => x.ChecklistEtapaId,
                        principalTable: "ChecklistEtapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemListaPreco",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListaPrecoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemListaPreco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemListaPreco_ListaPreco_ListaPrecoId",
                        column: x => x.ListaPrecoId,
                        principalTable: "ListaPreco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemListaPreco_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GraficoRelatorio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PainelRelatorioId = table.Column<long>(type: "bigint", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Agregacao = table.Column<string>(type: "text", nullable: false),
                    CampoValor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CampoAgrupamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FiltroJson = table.Column<string>(type: "text", nullable: true),
                    PeriodoCampo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PeriodoRelativo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Largura = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraficoRelatorio", x => x.Id);
                    table.CheckConstraint("CK_GraficoRelatorio_Largura", "\"Largura\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_GraficoRelatorio_PainelRelatorio_PainelRelatorioId",
                        column: x => x.PainelRelatorioId,
                        principalTable: "PainelRelatorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostaChecklistNegocio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NegocioId = table.Column<long>(type: "bigint", nullable: false),
                    ItemChecklistEtapaId = table.Column<long>(type: "bigint", nullable: false),
                    Concluido = table.Column<bool>(type: "boolean", nullable: false),
                    ConcluidoPorUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    ConcluidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostaChecklistNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostaChecklistNegocio_ItemChecklistEtapa_ItemChecklistEt~",
                        column: x => x.ItemChecklistEtapaId,
                        principalTable: "ItemChecklistEtapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespostaChecklistNegocio_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespostaChecklistNegocio_Usuario_ConcluidoPorUsuarioId",
                        column: x => x.ConcluidoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAprovacao_SolicitadoPorId",
                table: "SolicitacaoAprovacao",
                column: "SolicitadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEtapa_EtapaFunilId_Ordem",
                table: "ChecklistEtapa",
                columns: new[] { "EtapaFunilId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_GraficoRelatorio_PainelRelatorioId_Ordem",
                table: "GraficoRelatorio",
                columns: new[] { "PainelRelatorioId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_Importacao_EmpresaId_CriadoEm",
                table: "Importacao",
                columns: new[] { "EmpresaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Importacao_UsuarioId",
                table: "Importacao",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemChecklistEtapa_ChecklistEtapaId_Ordem",
                table: "ItemChecklistEtapa",
                columns: new[] { "ChecklistEtapaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemListaPreco_ListaPrecoId_ProdutoId",
                table: "ItemListaPreco",
                columns: new[] { "ListaPrecoId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemListaPreco_ProdutoId",
                table: "ItemListaPreco",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemNegocio_NegocioId_Ordem",
                table: "ItemNegocio",
                columns: new[] { "NegocioId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemNegocio_ProdutoId",
                table: "ItemNegocio",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ListaPreco_EmpresaId_Nome",
                table: "ListaPreco",
                columns: new[] { "EmpresaId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ListaPreco_MoedaId",
                table: "ListaPreco",
                column: "MoedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_EmpresaId_Inicio_Fim",
                table: "Meta",
                columns: new[] { "EmpresaId", "Inicio", "Fim" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_EquipeId",
                table: "Meta",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_FunilId",
                table: "Meta",
                column: "FunilId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_MoedaId",
                table: "Meta",
                column: "MoedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_UsuarioId",
                table: "Meta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PainelRelatorio_CriadoPorUsuarioId",
                table: "PainelRelatorio",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PainelRelatorio_EmpresaId_Ordem",
                table: "PainelRelatorio",
                columns: new[] { "EmpresaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissaoPerfil_EmpresaId_Perfil_Entidade",
                table: "PermissaoPerfil",
                columns: new[] { "EmpresaId", "Perfil", "Entidade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespostaChecklistNegocio_ConcluidoPorUsuarioId",
                table: "RespostaChecklistNegocio",
                column: "ConcluidoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaChecklistNegocio_ItemChecklistEtapaId",
                table: "RespostaChecklistNegocio",
                column: "ItemChecklistEtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaChecklistNegocio_NegocioId_ItemChecklistEtapaId",
                table: "RespostaChecklistNegocio",
                columns: new[] { "NegocioId", "ItemChecklistEtapaId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitacaoAprovacao_Usuario_SolicitadoPorId",
                table: "SolicitacaoAprovacao",
                column: "SolicitadoPorId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitacaoAprovacao_Usuario_SolicitadoPorId",
                table: "SolicitacaoAprovacao");

            migrationBuilder.DropTable(
                name: "GraficoRelatorio");

            migrationBuilder.DropTable(
                name: "Importacao");

            migrationBuilder.DropTable(
                name: "ItemListaPreco");

            migrationBuilder.DropTable(
                name: "ItemNegocio");

            migrationBuilder.DropTable(
                name: "Meta");

            migrationBuilder.DropTable(
                name: "PermissaoPerfil");

            migrationBuilder.DropTable(
                name: "RespostaChecklistNegocio");

            migrationBuilder.DropTable(
                name: "PainelRelatorio");

            migrationBuilder.DropTable(
                name: "ListaPreco");

            migrationBuilder.DropTable(
                name: "ItemChecklistEtapa");

            migrationBuilder.DropTable(
                name: "ChecklistEtapa");

            migrationBuilder.DropIndex(
                name: "IX_SolicitacaoAprovacao_SolicitadoPorId",
                table: "SolicitacaoAprovacao");

            migrationBuilder.DropColumn(
                name: "ComentarioDecisao",
                table: "SolicitacaoAprovacao");

            migrationBuilder.DropColumn(
                name: "SolicitadoPorId",
                table: "SolicitacaoAprovacao");
        }
    }
}

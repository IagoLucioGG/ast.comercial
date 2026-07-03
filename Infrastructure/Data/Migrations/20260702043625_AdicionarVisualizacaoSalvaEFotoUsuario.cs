using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVisualizacaoSalvaEFotoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Usuario",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VisualizacaoSalva",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    EntidadeAlvo = table.Column<string>(type: "text", nullable: false),
                    Filtro = table.Column<string>(type: "text", nullable: true),
                    Colunas = table.Column<string>(type: "text", nullable: true),
                    Ordenacao = table.Column<string>(type: "text", nullable: true),
                    ItensPorPagina = table.Column<int>(type: "integer", nullable: false),
                    UsuariosVisiveis = table.Column<string>(type: "text", nullable: true),
                    CriadoPorId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualizacaoSalva", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisualizacaoSalva");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Usuario");
        }
    }
}

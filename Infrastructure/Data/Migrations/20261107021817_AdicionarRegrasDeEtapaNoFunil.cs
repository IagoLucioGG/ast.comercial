using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRegrasDeEtapaNoFunil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExigirTodasEtapas",
                table: "Funil",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProibirRetornoEtapa",
                table: "Funil",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Obs.: o EF também detectou ModeloDocumento/SecaoModelo ausentes do snapshot anterior.
            // Essas tabelas já são criadas (IF NOT EXISTS) no Program.cs em Development e já existem no banco,
            // então não são recriadas aqui; o snapshot foi apenas sincronizado com o modelo.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExigirTodasEtapas",
                table: "Funil");

            migrationBuilder.DropColumn(
                name: "ProibirRetornoEtapa",
                table: "Funil");
        }
    }
}

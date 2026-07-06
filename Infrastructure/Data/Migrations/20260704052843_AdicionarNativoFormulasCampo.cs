using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNativoFormulasCampo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Campo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Formula",
                table: "Campo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Chave",
                table: "Campo",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "FormulaObrigatoriedade",
                table: "Campo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaSomenteLeitura",
                table: "Campo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaVisibilidade",
                table: "Campo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Nativo",
                table: "Campo",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormulaObrigatoriedade",
                table: "Campo");

            migrationBuilder.DropColumn(
                name: "FormulaSomenteLeitura",
                table: "Campo");

            migrationBuilder.DropColumn(
                name: "FormulaVisibilidade",
                table: "Campo");

            migrationBuilder.DropColumn(
                name: "Nativo",
                table: "Campo");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Campo",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Formula",
                table: "Campo",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Chave",
                table: "Campo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);
        }
    }
}

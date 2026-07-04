using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPermiteMultiplosValoresCampo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermiteMultiplosValores",
                table: "Campo",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermiteMultiplosValores",
                table: "Campo");
        }
    }
}

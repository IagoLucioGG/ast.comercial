using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtapaPedido",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapaPedido", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CustoFrete = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NumeroParcelas = table.Column<int>(type: "integer", nullable: true),
                    FormaPagamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    NegocioId = table.Column<long>(type: "bigint", nullable: true),
                    PropostaId = table.Column<long>(type: "bigint", nullable: true),
                    EtapaPedidoId = table.Column<long>(type: "bigint", nullable: true),
                    ResponsavelId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedido_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pedido_EtapaPedido_EtapaPedidoId",
                        column: x => x.EtapaPedidoId,
                        principalTable: "EtapaPedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pedido_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pedido_Proposta_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Proposta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemPedido",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ProdutoCodigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantidade = table.Column<double>(type: "double precision", nullable: true),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Desconto = table.Column<double>(type: "double precision", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    MoedaId = table.Column<long>(type: "bigint", nullable: true),
                    EmpresaId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveExterna = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPedido_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPedido_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtapaPedido_EmpresaId_Ordem",
                table: "EtapaPedido",
                columns: new[] { "EmpresaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPedido_PedidoId_Ordem",
                table: "ItemPedido",
                columns: new[] { "PedidoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPedido_ProdutoId",
                table: "ItemPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_ClienteId",
                table: "Pedido",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_EmpresaId_Numero",
                table: "Pedido",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_EtapaPedidoId",
                table: "Pedido",
                column: "EtapaPedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_NegocioId",
                table: "Pedido",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_PropostaId",
                table: "Pedido",
                column: "PropostaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemPedido");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "EtapaPedido");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderChangeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Campo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ValorAnterior = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValorNuevo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderChangeLogs_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderChangeLogs_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_FechaUtc",
                table: "OrderChangeLogs",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_ProductionOrderId",
                table: "OrderChangeLogs",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_UsuarioId",
                table: "OrderChangeLogs",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderChangeLogs");
        }
    }
}

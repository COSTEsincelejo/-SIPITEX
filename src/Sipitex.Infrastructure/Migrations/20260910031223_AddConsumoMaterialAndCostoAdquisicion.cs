using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumoMaterialAndCostoAdquisicion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "StockMovements",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoAdquisicion",
                table: "Materials",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ConsumosMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    GrupoConfeccionId = table.Column<int>(type: "INTEGER", nullable: true),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResponsableUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterial_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterial_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterial_Users_ResponsableUserId",
                        column: x => x.ResponsableUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_FechaUtc",
                table: "ConsumosMaterial",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_MaterialId",
                table: "ConsumosMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_ProductionOrderId",
                table: "ConsumosMaterial",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_ResponsableUserId",
                table: "ConsumosMaterial",
                column: "ResponsableUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosMaterial");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CostoAdquisicion",
                table: "Materials");
        }
    }
}

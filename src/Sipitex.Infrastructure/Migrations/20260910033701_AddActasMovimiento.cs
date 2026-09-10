using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActasMovimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActasMovimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Origen = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    EstadoProductoOrigen = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    EstadoProductoDestino = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    EntregaNombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EntregaCargo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntregaConformidadUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecibeNombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RecibeCargo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RecibeConformidadUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPorUserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasMovimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActasMovimiento_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActasMovimiento_Users_CreadoPorUserId",
                        column: x => x.CreadoPorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActasMovimientoDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActaMovimientoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemTipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConsumoMaterialId = table.Column<int>(type: "INTEGER", nullable: true),
                    StockMovementId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Cantidad = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Unidad = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasMovimientoDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_ActasMovimiento_ActaMovimientoId",
                        column: x => x.ActaMovimientoId,
                        principalTable: "ActasMovimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_ConsumosMaterial_ConsumoMaterialId",
                        column: x => x.ConsumoMaterialId,
                        principalTable: "ConsumosMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_CreadoPorUserId",
                table: "ActasMovimiento",
                column: "CreadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_FechaUtc",
                table: "ActasMovimiento",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_Numero",
                table: "ActasMovimiento",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_ProductionOrderId",
                table: "ActasMovimiento",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ActaMovimientoId",
                table: "ActasMovimientoDetalle",
                column: "ActaMovimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ConsumoMaterialId",
                table: "ActasMovimientoDetalle",
                column: "ConsumoMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_MaterialId",
                table: "ActasMovimientoDetalle",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ProductionOrderId",
                table: "ActasMovimientoDetalle",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_StockMovementId",
                table: "ActasMovimientoDetalle",
                column: "StockMovementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActasMovimientoDetalle");

            migrationBuilder.DropTable(
                name: "ActasMovimiento");
        }
    }
}

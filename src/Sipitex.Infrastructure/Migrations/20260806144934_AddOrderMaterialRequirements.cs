using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderMaterialRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaterialsStatus",
                table: "ProductionOrders",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProductionOrderMaterialRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    QuantityDelivered = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false),
                    Observations = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderMaterialRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderMaterialRequirements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderMaterialRequirements_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_MaterialId",
                table: "ProductionOrderMaterialRequirements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_ProductionOrderId",
                table: "ProductionOrderMaterialRequirements",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_ProductionOrderId_MaterialId",
                table: "ProductionOrderMaterialRequirements",
                columns: new[] { "ProductionOrderId", "MaterialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOrderMaterialRequirements");

            migrationBuilder.DropColumn(
                name: "MaterialsStatus",
                table: "ProductionOrders");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConsumoYCostoEntregaOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoEntregadoAcumulado",
                table: "ProductionOrderMaterialRequirements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityConsumed",
                table: "ProductionOrderMaterialRequirements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoEntregadoAcumulado",
                table: "ProductionOrderMaterialRequirements");

            migrationBuilder.DropColumn(
                name: "QuantityConsumed",
                table: "ProductionOrderMaterialRequirements");
        }
    }
}

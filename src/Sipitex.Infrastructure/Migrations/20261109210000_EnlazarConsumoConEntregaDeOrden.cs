using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sipitex.Infrastructure.Persistence;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SipitexDbContext))]
    [Migration("20261109210000_EnlazarConsumoConEntregaDeOrden")]
    public partial class EnlazarConsumoConEntregaDeOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderMaterialRequirementId",
                table: "ConsumosMaterial",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_OrderMaterialRequirementId",
                table: "ConsumosMaterial",
                column: "OrderMaterialRequirementId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumosMaterial_ReqMaterialOrden",
                table: "ConsumosMaterial",
                column: "OrderMaterialRequirementId",
                principalTable: "ProductionOrderMaterialRequirements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumosMaterial_ReqMaterialOrden",
                table: "ConsumosMaterial");

            migrationBuilder.DropIndex(
                name: "IX_ConsumosMaterial_OrderMaterialRequirementId",
                table: "ConsumosMaterial");

            migrationBuilder.DropColumn(
                name: "OrderMaterialRequirementId",
                table: "ConsumosMaterial");
        }
    }
}

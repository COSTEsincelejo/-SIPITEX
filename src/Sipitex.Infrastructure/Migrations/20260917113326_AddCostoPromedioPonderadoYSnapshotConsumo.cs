using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostoPromedioPonderadoYSnapshotConsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CostoUnitario",
                table: "ConsumosMaterial",
                newName: "CostoUnitarioAlMomento");

            migrationBuilder.AddColumn<decimal>(
                name: "CostoPromedioPonderado",
                table: "Materials",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Insumos existentes sin historial de compras: parte del precio actual usado hoy en costeo.
            migrationBuilder.Sql(
                """
                UPDATE "Materials"
                SET "CostoPromedioPonderado" = "CostoAdquisicion";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoPromedioPonderado",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "CostoUnitarioAlMomento",
                table: "ConsumosMaterial",
                newName: "CostoUnitario");
        }
    }
}

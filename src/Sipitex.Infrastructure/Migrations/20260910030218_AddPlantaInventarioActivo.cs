using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantaInventarioActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "PlantasInventario",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "PlantasInventario",
                keyColumn: "Id",
                keyValue: 1,
                column: "Activo",
                value: true);

            migrationBuilder.UpdateData(
                table: "PlantasInventario",
                keyColumn: "Id",
                keyValue: 2,
                column: "Activo",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "PlantasInventario");
        }
    }
}

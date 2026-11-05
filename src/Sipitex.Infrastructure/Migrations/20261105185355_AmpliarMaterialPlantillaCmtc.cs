using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarMaterialPlantillaCmtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Materials",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoAlmacen",
                table: "Materials",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Materials",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                table: "Materials",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "Materials",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subcategoria",
                table: "Materials",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoElemento",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Code",
                table: "Materials",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CodigoAlmacen",
                table: "Materials",
                column: "CodigoAlmacen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_Code",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_CodigoAlmacen",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "CodigoAlmacen",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Proveedor",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Subcategoria",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "TipoElemento",
                table: "Materials");
        }
    }
}

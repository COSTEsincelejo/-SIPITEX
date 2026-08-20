using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBodegas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear Bodegas y seed antes de las FKs (mismo orden que CurrentStageId en AddProductionOrderMesFlow).
            migrationBuilder.CreateTable(
                name: "Bodegas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodegas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Bodegas",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Bodega 1" },
                    { 2, "Bodega 2" }
                });

            migrationBuilder.AddColumn<int>(
                name: "BodegaId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            // DEFAULT 1: backfill de filas existentes a "Bodega 1" (mismo patrón que Tipo="PorFicha").
            migrationBuilder.AddColumn<int>(
                name: "BodegaId",
                table: "SolicitudesMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BodegaId",
                table: "Materials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BodegaId",
                table: "Users",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_BodegaId",
                table: "SolicitudesMaterial",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_BodegaId",
                table: "Materials",
                column: "BodegaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Bodegas_BodegaId",
                table: "Materials",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesMaterial_Bodegas_BodegaId",
                table: "SolicitudesMaterial",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Bodegas_BodegaId",
                table: "Users",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Bodegas_BodegaId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesMaterial_Bodegas_BodegaId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Bodegas_BodegaId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Bodegas");

            migrationBuilder.DropIndex(
                name: "IX_Users_BodegaId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesMaterial_BodegaId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropIndex(
                name: "IX_Materials_BodegaId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "Materials");
        }
    }
}

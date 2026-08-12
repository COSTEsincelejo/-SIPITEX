using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBomProductMetadataAndTallas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnioMuestrario",
                table: "BomProducts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionPrenda",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Digitacion",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Disenador",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsBancoDeMuestras",
                table: "BomProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsDisenoNuevo",
                table: "BomProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsReplica",
                table: "BomProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaElaboracion",
                table: "BomProducts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaSolicitud",
                table: "BomProducts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Linea",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Patronista",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TallaInicial",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoEmpaque",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BomProductTallas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BomProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductTallas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductTallas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomProductTallas_BomProductId",
                table: "BomProductTallas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductTallas_BomProductId_Orden",
                table: "BomProductTallas",
                columns: new[] { "BomProductId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomProductTallas");

            migrationBuilder.DropColumn(
                name: "AnioMuestrario",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "DescripcionPrenda",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Digitacion",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Disenador",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "EsBancoDeMuestras",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "EsDisenoNuevo",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "EsReplica",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "FechaElaboracion",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "FechaSolicitud",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Linea",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Patronista",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "TallaInicial",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "TipoEmpaque",
                table: "BomProducts");
        }
    }
}

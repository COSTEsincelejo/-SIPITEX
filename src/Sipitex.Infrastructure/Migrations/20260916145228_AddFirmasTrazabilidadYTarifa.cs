using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmasTrazabilidadYTarifa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EntregaFirmaPng",
                table: "ActasMovimiento",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RecibeFirmaPng",
                table: "ActasMovimiento",
                type: "BLOB",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "PrendasTrazables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Talla = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrendasTrazables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrendasTrazables_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrendasTrazables_Users_CreadoPorUserId",
                        column: x => x.CreadoPorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_Codigo",
                table: "PrendasTrazables",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_CreadoPorUserId",
                table: "PrendasTrazables",
                column: "CreadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_CreadoUtc",
                table: "PrendasTrazables",
                column: "CreadoUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_ProductionOrderId",
                table: "PrendasTrazables",
                column: "ProductionOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "PrendasTrazables");

            migrationBuilder.DropColumn(
                name: "EntregaFirmaPng",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "RecibeFirmaPng",
                table: "ActasMovimiento");
        }
    }
}

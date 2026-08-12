using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBomProductPatronajePiezasMedidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BomProductMedidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BomProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tolerancia = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ComoMedir = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductMedidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductMedidas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomProductPiezas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BomProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Tela = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductPiezas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductPiezas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomProductMedidaValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BomProductMedidaId = table.Column<int>(type: "INTEGER", nullable: false),
                    BomProductTallaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductMedidaValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductMedidaValores_BomProductMedidas_BomProductMedidaId",
                        column: x => x.BomProductMedidaId,
                        principalTable: "BomProductMedidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomProductMedidaValores_BomProductTallas_BomProductTallaId",
                        column: x => x.BomProductTallaId,
                        principalTable: "BomProductTallas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidas_BomProductId",
                table: "BomProductMedidas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidas_BomProductId_Tipo_Orden",
                table: "BomProductMedidas",
                columns: new[] { "BomProductId", "Tipo", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidaValores_BomProductMedidaId_BomProductTallaId",
                table: "BomProductMedidaValores",
                columns: new[] { "BomProductMedidaId", "BomProductTallaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidaValores_BomProductTallaId",
                table: "BomProductMedidaValores",
                column: "BomProductTallaId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductPiezas_BomProductId",
                table: "BomProductPiezas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductPiezas_BomProductId_Orden",
                table: "BomProductPiezas",
                columns: new[] { "BomProductId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomProductMedidaValores");

            migrationBuilder.DropTable(
                name: "BomProductPiezas");

            migrationBuilder.DropTable(
                name: "BomProductMedidas");
        }
    }
}

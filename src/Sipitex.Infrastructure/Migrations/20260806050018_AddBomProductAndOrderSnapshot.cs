using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBomProductAndOrderSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BomProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsReference = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HabilitadoParaOrdenes = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProducts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomProducts_ProductName",
                table: "BomProducts",
                column: "ProductName",
                unique: true);

            // Cabeceras a partir de ProductName ya existentes en BomItems
            migrationBuilder.Sql("""
                INSERT INTO BomProducts (ProductName, IsReference, Notes, HabilitadoParaOrdenes)
                SELECT DISTINCT ProductName, 0, NULL, 1 FROM BomItems;
                """);

            migrationBuilder.AddColumn<int>(
                name: "BomProductId",
                table: "BomItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE BomItems
                SET BomProductId = (
                    SELECT Id FROM BomProducts
                    WHERE BomProducts.ProductName = BomItems.ProductName
                );
                """);

            // SQLite: recrear BomItems con BomProductId NOT NULL
            migrationBuilder.Sql("""
                CREATE TABLE BomItems_new (
                    Id INTEGER NOT NULL CONSTRAINT PK_BomItems PRIMARY KEY AUTOINCREMENT,
                    BomProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    MaterialId INTEGER NOT NULL,
                    QuantityPerUnit TEXT NOT NULL,
                    Unit INTEGER NOT NULL,
                    CONSTRAINT FK_BomItems_BomProducts_BomProductId FOREIGN KEY (BomProductId) REFERENCES BomProducts (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_BomItems_Materials_MaterialId FOREIGN KEY (MaterialId) REFERENCES Materials (Id) ON DELETE CASCADE
                );
                INSERT INTO BomItems_new (Id, BomProductId, ProductName, MaterialId, QuantityPerUnit, Unit)
                SELECT Id, BomProductId, ProductName, MaterialId, QuantityPerUnit, Unit FROM BomItems WHERE BomProductId IS NOT NULL;
                DROP TABLE BomItems;
                ALTER TABLE BomItems_new RENAME TO BomItems;
                CREATE INDEX IX_BomItems_BomProductId ON BomItems (BomProductId);
                CREATE INDEX IX_BomItems_MaterialId ON BomItems (MaterialId);
                """);

            migrationBuilder.CreateTable(
                name: "ProductionOrderBomSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MaterialName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderBomSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderBomSnapshots_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderBomSnapshots_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderBomSnapshots_MaterialId",
                table: "ProductionOrderBomSnapshots",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderBomSnapshots_ProductionOrderId",
                table: "ProductionOrderBomSnapshots",
                column: "ProductionOrderId");

            // Congelar BOM vigente para órdenes ya existentes
            migrationBuilder.Sql("""
                INSERT INTO ProductionOrderBomSnapshots (ProductionOrderId, MaterialId, MaterialCode, MaterialName, QuantityPerUnit, Unit)
                SELECT o.Id, b.MaterialId, m.Code, m.Name, b.QuantityPerUnit, b.Unit
                FROM ProductionOrders o
                INNER JOIN BomItems b ON b.ProductName = o.ProductName
                INNER JOIN Materials m ON m.Id = b.MaterialId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductionOrderBomSnapshots");

            migrationBuilder.Sql("""
                CREATE TABLE BomItems_old (
                    Id INTEGER NOT NULL CONSTRAINT PK_BomItems PRIMARY KEY AUTOINCREMENT,
                    ProductName TEXT NOT NULL,
                    MaterialId INTEGER NOT NULL,
                    QuantityPerUnit TEXT NOT NULL,
                    Unit INTEGER NOT NULL,
                    CONSTRAINT FK_BomItems_Materials_MaterialId FOREIGN KEY (MaterialId) REFERENCES Materials (Id) ON DELETE CASCADE
                );
                INSERT INTO BomItems_old (Id, ProductName, MaterialId, QuantityPerUnit, Unit)
                SELECT Id, ProductName, MaterialId, QuantityPerUnit, Unit FROM BomItems;
                DROP TABLE BomItems;
                ALTER TABLE BomItems_old RENAME TO BomItems;
                CREATE INDEX IX_BomItems_MaterialId ON BomItems (MaterialId);
                """);

            migrationBuilder.DropTable(name: "BomProducts");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarcarStockSinCosto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SinCosto",
                table: "StockMovements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "StockSinCosto",
                table: "Materials",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Las importaciones iniciales ya cargadas a costo 0 quedan fuera del próximo promedio.
            migrationBuilder.Sql("""
                UPDATE "StockMovements"
                SET "SinCosto" = TRUE
                WHERE "Referencia" = 'Importación inicial'
                  AND "TipoMovimiento" = 'Entrada'
                  AND COALESCE("CostoUnitario", 0) = 0;

                UPDATE "Materials" AS m
                SET "StockSinCosto" = LEAST(m."Stock", s.qty)
                FROM (
                    SELECT "MaterialId", SUM("Cantidad") AS qty
                    FROM "StockMovements"
                    WHERE "SinCosto" = TRUE
                    GROUP BY "MaterialId"
                ) AS s
                WHERE m."Id" = s."MaterialId"
                  AND m."CostoPromedioPonderado" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SinCosto",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "StockSinCosto",
                table: "Materials");
        }
    }
}

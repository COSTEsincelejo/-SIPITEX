using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrenciaStockYNumeracionOrdenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "Materials",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "NumeracionesDocumento",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Siguiente = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumeracionesDocumento", x => x.Codigo);
                });

            // El próximo OP-xxx arranca en 101 (la serie histórica de Count + 101)
            // o un número después del mayor OP- que ya exista. No reescribe órdenes ni stock.
            migrationBuilder.Sql(
                """
                INSERT INTO "NumeracionesDocumento" ("Codigo", "Siguiente")
                SELECT 'OrdenProduccion',
                       GREATEST(
                         101,
                         COALESCE(
                           (SELECT MAX(CAST(SUBSTRING("OrderNumber" FROM 4) AS integer)) + 1
                            FROM "ProductionOrders"
                            WHERE "OrderNumber" ~ '^OP-[0-9]+$'),
                           101));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumeracionesDocumento");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Materials");
        }
    }
}

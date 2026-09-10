using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGruposCalidadEstadosCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Clasificacion",
                table: "QualityRecords",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Bueno");

            migrationBuilder.AddColumn<string>(
                name: "EstadoProducto",
                table: "ProductionOrders",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "MateriaPrima");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "BomProducts",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            if (string.Equals(migrationBuilder.ActiveProvider, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "BomProducts"
                    SET "Codigo" = 'PRD-' || printf('%05d', "Id")
                    WHERE "Codigo" IS NULL OR "Codigo" = '';
                    """);
            }
            else
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "BomProducts"
                    SET "Codigo" = 'PRD-' || lpad("Id"::text, 5, '0')
                    WHERE "Codigo" IS NULL OR "Codigo" = '';
                    """);
            }

            migrationBuilder.CreateTable(
                name: "GruposConfeccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    InstructorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaRealizacion = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    CantidadPrendas = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposConfeccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposConfeccion_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GruposConfeccion_Users_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_GrupoConfeccionId",
                table: "ConsumosMaterial",
                column: "GrupoConfeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProducts_Codigo",
                table: "BomProducts",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_FechaRealizacion",
                table: "GruposConfeccion",
                column: "FechaRealizacion");

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_InstructorUserId",
                table: "GruposConfeccion",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_ProductionOrderId",
                table: "GruposConfeccion",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumosMaterial_GruposConfeccion_GrupoConfeccionId",
                table: "ConsumosMaterial",
                column: "GrupoConfeccionId",
                principalTable: "GruposConfeccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumosMaterial_GruposConfeccion_GrupoConfeccionId",
                table: "ConsumosMaterial");

            migrationBuilder.DropTable(
                name: "GruposConfeccion");

            migrationBuilder.DropIndex(
                name: "IX_ConsumosMaterial_GrupoConfeccionId",
                table: "ConsumosMaterial");

            migrationBuilder.DropIndex(
                name: "IX_BomProducts_Codigo",
                table: "BomProducts");

            migrationBuilder.DropColumn(
                name: "Clasificacion",
                table: "QualityRecords");

            migrationBuilder.DropColumn(
                name: "EstadoProducto",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "BomProducts");
        }
    }
}

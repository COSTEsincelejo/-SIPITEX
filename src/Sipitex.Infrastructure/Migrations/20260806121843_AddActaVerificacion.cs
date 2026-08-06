using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActaVerificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActasVerificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    FichaId = table.Column<int>(type: "INTEGER", nullable: false),
                    InstructorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CumpleEspecificaciones = table.Column<bool>(type: "INTEGER", nullable: false),
                    CumpleAcabados = table.Column<bool>(type: "INTEGER", nullable: false),
                    CumpleSinDefectos = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecklistCumpleRequisitos = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaObservacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Firmado = table.Column<bool>(type: "INTEGER", nullable: false),
                    NombreFirmante = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasVerificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActasVerificacion_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActasVerificacion_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActasVerificacion_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActasVerificacion_FichaId",
                table: "ActasVerificacion",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasVerificacion_InstructorId",
                table: "ActasVerificacion",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasVerificacion_ProductionOrderId",
                table: "ActasVerificacion",
                column: "ProductionOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActasVerificacion");
        }
    }
}

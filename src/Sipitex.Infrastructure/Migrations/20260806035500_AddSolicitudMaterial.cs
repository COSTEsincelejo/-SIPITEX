using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FichaId = table.Column<int>(type: "INTEGER", nullable: false),
                    SolicitanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResueltoPorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Users_ResueltoPorId",
                        column: x => x.ResueltoPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Users_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesSolicitudMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadSolicitada = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CantidadAprobada = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    EstadoItem = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesSolicitudMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesSolicitudMaterial_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesSolicitudMaterial_SolicitudesMaterial_SolicitudMaterialId",
                        column: x => x.SolicitudMaterialId,
                        principalTable: "SolicitudesMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregasMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SolicitudMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    BodegueroId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasMaterial_SolicitudesMaterial_SolicitudMaterialId",
                        column: x => x.SolicitudMaterialId,
                        principalTable: "SolicitudesMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntregasMaterial_Users_BodegueroId",
                        column: x => x.BodegueroId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSolicitudMaterial_MaterialId",
                table: "DetallesSolicitudMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSolicitudMaterial_SolicitudMaterialId",
                table: "DetallesSolicitudMaterial",
                column: "SolicitudMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_BodegueroId",
                table: "EntregasMaterial",
                column: "BodegueroId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_Codigo",
                table: "EntregasMaterial",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_SolicitudMaterialId",
                table: "EntregasMaterial",
                column: "SolicitudMaterialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Codigo",
                table: "SolicitudesMaterial",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Estado",
                table: "SolicitudesMaterial",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_FichaId",
                table: "SolicitudesMaterial",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_ResueltoPorId",
                table: "SolicitudesMaterial",
                column: "ResueltoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_SolicitanteId",
                table: "SolicitudesMaterial",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesSolicitudMaterial");

            migrationBuilder.DropTable(
                name: "EntregasMaterial");

            migrationBuilder.DropTable(
                name: "SolicitudesMaterial");
        }
    }
}

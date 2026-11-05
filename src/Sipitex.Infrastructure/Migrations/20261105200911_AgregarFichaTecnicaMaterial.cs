using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFichaTecnicaMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FichasTecnicasMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    DescripcionTecnica = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TipoMaterial = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Composicion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GramajeGm2 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AnchoCm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Colores = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Acabados = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EspecificacionesUso = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcesoFabricacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NormasCertificaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnlaceFichaOriginal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasTecnicasMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichasTecnicasMaterial_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicasMaterial_MaterialId",
                table: "FichasTecnicasMaterial",
                column: "MaterialId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FichasTecnicasMaterial");
        }
    }
}

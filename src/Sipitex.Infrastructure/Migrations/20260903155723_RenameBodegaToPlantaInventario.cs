using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <summary>
    /// Renombra Bodega → PlantaInventario y Bodeguero → EncargadoBodega sin recrear datos.
    ///
    /// Cambios de SCHEMA (SQLite: RenameTable / RenameColumn disponibles desde EF Core 6):
    ///   • Tabla  Bodegas                  → PlantasInventario
    ///   • Tabla  UserBodegas              → UserPlantasInventario
    ///   • Columna Materials.BodegaId      → PlantaInventarioId
    ///   • Columna SolicitudesMaterial.BodegaId → PlantaInventarioId
    ///   • Columna UserPlantasInventario.BodegaId → PlantaInventarioId
    ///   • Columna EntregasMaterial.BodegueroId → EncargadoBodegaId
    ///
    /// Cambios de DATOS (fila por fila en UPDATE):
    ///   • Users.Rol: 'Bodeguero' → 'EncargadoBodega'
    ///   • PlantasInventario.Nombre: 'Bodega 1' → 'Planta de Inventario 1',
    ///                                'Bodega 2' → 'Planta de Inventario 2'
    ///
    /// Down() invierte exactamente los mismos cambios para ser reversible.
    /// </summary>
    public partial class RenameBodegaToPlantaInventario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Renombrar tablas ──────────────────────────────────────────────
            migrationBuilder.RenameTable(
                name: "Bodegas",
                newName: "PlantasInventario");

            migrationBuilder.RenameTable(
                name: "UserBodegas",
                newName: "UserPlantasInventario");

            // ── 2. Renombrar columnas FK ─────────────────────────────────────────
            migrationBuilder.RenameColumn(
                name: "BodegaId",
                table: "Materials",
                newName: "PlantaInventarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_BodegaId",
                table: "Materials",
                newName: "IX_Materials_PlantaInventarioId");

            migrationBuilder.RenameColumn(
                name: "BodegaId",
                table: "SolicitudesMaterial",
                newName: "PlantaInventarioId");

            migrationBuilder.RenameIndex(
                name: "IX_SolicitudesMaterial_BodegaId",
                table: "SolicitudesMaterial",
                newName: "IX_SolicitudesMaterial_PlantaInventarioId");

            migrationBuilder.RenameColumn(
                name: "BodegaId",
                table: "UserPlantasInventario",
                newName: "PlantaInventarioId");

            migrationBuilder.RenameIndex(
                name: "IX_UserBodegas_BodegaId",
                table: "UserPlantasInventario",
                newName: "IX_UserPlantasInventario_PlantaInventarioId");

            migrationBuilder.RenameColumn(
                name: "BodegueroId",
                table: "EntregasMaterial",
                newName: "EncargadoBodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_EntregasMaterial_BodegueroId",
                table: "EntregasMaterial",
                newName: "IX_EntregasMaterial_EncargadoBodegaId");

            // ── 3. Actualizar datos: rol de usuario ──────────────────────────────
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Rol" = 'EncargadoBodega'
                WHERE "Rol" = 'Bodeguero';
                """);

            // ── 4. Actualizar datos: nombres de plantas de inventario ─────────────
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario"
                SET "Nombre" = 'Planta de Inventario 1'
                WHERE "Nombre" = 'Bodega 1';
                """);

            migrationBuilder.Sql("""
                UPDATE "PlantasInventario"
                SET "Nombre" = 'Planta de Inventario 2'
                WHERE "Nombre" = 'Bodega 2';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── 4. Revertir datos: nombres ───────────────────────────────────────
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario"
                SET "Nombre" = 'Bodega 2'
                WHERE "Nombre" = 'Planta de Inventario 2';
                """);

            migrationBuilder.Sql("""
                UPDATE "PlantasInventario"
                SET "Nombre" = 'Bodega 1'
                WHERE "Nombre" = 'Planta de Inventario 1';
                """);

            // ── 3. Revertir datos: rol de usuario ────────────────────────────────
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Rol" = 'Bodeguero'
                WHERE "Rol" = 'EncargadoBodega';
                """);

            // ── 2. Revertir columnas FK ──────────────────────────────────────────
            migrationBuilder.RenameIndex(
                name: "IX_EntregasMaterial_EncargadoBodegaId",
                table: "EntregasMaterial",
                newName: "IX_EntregasMaterial_BodegueroId");

            migrationBuilder.RenameColumn(
                name: "EncargadoBodegaId",
                table: "EntregasMaterial",
                newName: "BodegueroId");

            migrationBuilder.RenameIndex(
                name: "IX_UserPlantasInventario_PlantaInventarioId",
                table: "UserPlantasInventario",
                newName: "IX_UserBodegas_BodegaId");

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "UserPlantasInventario",
                newName: "BodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_SolicitudesMaterial_PlantaInventarioId",
                table: "SolicitudesMaterial",
                newName: "IX_SolicitudesMaterial_BodegaId");

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "SolicitudesMaterial",
                newName: "BodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_PlantaInventarioId",
                table: "Materials",
                newName: "IX_Materials_BodegaId");

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "Materials",
                newName: "BodegaId");

            // ── 1. Revertir tablas ───────────────────────────────────────────────
            migrationBuilder.RenameTable(
                name: "UserPlantasInventario",
                newName: "UserBodegas");

            migrationBuilder.RenameTable(
                name: "PlantasInventario",
                newName: "Bodegas");
        }
    }
}

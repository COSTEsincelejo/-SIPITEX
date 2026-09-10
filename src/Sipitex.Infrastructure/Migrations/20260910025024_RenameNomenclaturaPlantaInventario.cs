using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <summary>
    /// Renombra Bodega → PlantaInventario, UserBodegas → UserPlantasInventario,
    /// BodegueroId → EncargadoDeBodegaId y FichaCode → NumeroGrupo.
    /// Usa RenameTable/RenameColumn (no drop/create) para no perder datos.
    /// SQL de datos quoted, portable SQLite / PostgreSQL.
    /// </summary>
    public partial class RenameNomenclaturaPlantaInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntregasMaterial_Users_BodegueroId",
                table: "EntregasMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Bodegas_BodegaId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesMaterial_Bodegas_BodegaId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBodegas_Bodegas_BodegaId",
                table: "UserBodegas");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBodegas_Users_UserId",
                table: "UserBodegas");

            migrationBuilder.RenameTable(
                name: "Bodegas",
                newName: "PlantasInventario");

            migrationBuilder.RenameTable(
                name: "UserBodegas",
                newName: "UserPlantasInventario");

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
                table: "Materials",
                newName: "PlantaInventarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_BodegaId",
                table: "Materials",
                newName: "IX_Materials_PlantaInventarioId");

            migrationBuilder.RenameColumn(
                name: "FichaCode",
                table: "Fichas",
                newName: "NumeroGrupo");

            migrationBuilder.RenameColumn(
                name: "BodegueroId",
                table: "EntregasMaterial",
                newName: "EncargadoDeBodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_EntregasMaterial_BodegueroId",
                table: "EntregasMaterial",
                newName: "IX_EntregasMaterial_EncargadoDeBodegaId");

            migrationBuilder.RenameColumn(
                name: "BodegaId",
                table: "UserPlantasInventario",
                newName: "PlantaInventarioId");

            migrationBuilder.RenameIndex(
                name: "IX_UserBodegas_BodegaId",
                table: "UserPlantasInventario",
                newName: "IX_UserPlantasInventario_PlantaInventarioId");

            RenamePrimaryKeysForPostgres(migrationBuilder, up: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EntregasMaterial_Users_EncargadoDeBodegaId",
                table: "EntregasMaterial",
                column: "EncargadoDeBodegaId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_PlantasInventario_PlantaInventarioId",
                table: "Materials",
                column: "PlantaInventarioId",
                principalTable: "PlantasInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesMaterial_PlantasInventario_PlantaInventarioId",
                table: "SolicitudesMaterial",
                column: "PlantaInventarioId",
                principalTable: "PlantasInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPlantasInventario_PlantasInventario_PlantaInventarioId",
                table: "UserPlantasInventario",
                column: "PlantaInventarioId",
                principalTable: "PlantasInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPlantasInventario_Users_UserId",
                table: "UserPlantasInventario",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Semilla + rol + enum persistido como string. Quoted identifiers: SQLite y PostgreSQL.
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario" SET "Nombre" = 'Planta de Inventario 1'
                WHERE "Id" = 1 AND "Nombre" = 'Bodega 1';
                """);
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario" SET "Nombre" = 'Planta de Inventario 2'
                WHERE "Id" = 2 AND "Nombre" = 'Bodega 2';
                """);
            migrationBuilder.Sql("""
                UPDATE "Users" SET "Rol" = 'Encargado de bodega' WHERE "Rol" = 'Bodeguero';
                """);
            migrationBuilder.Sql("""
                UPDATE "ProductionOrders"
                SET "MaterialsStatus" = 'PendienteRevisionPlantaInventario'
                WHERE "MaterialsStatus" = 'PendienteRevisionBodega';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Entity" = 'PlantaInventario' WHERE "Entity" = 'Bodega';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'CreatePlantaInventario' WHERE "Action" = 'CreateBodega';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'UpdatePlantaInventario' WHERE "Action" = 'UpdateBodega';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'DeletePlantaInventario' WHERE "Action" = 'DeleteBodega';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'CreateBodega' WHERE "Action" = 'CreatePlantaInventario';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'UpdateBodega' WHERE "Action" = 'UpdatePlantaInventario';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Action" = 'DeleteBodega' WHERE "Action" = 'DeletePlantaInventario';
                """);
            migrationBuilder.Sql("""
                UPDATE "ActivityLogs" SET "Entity" = 'Bodega' WHERE "Entity" = 'PlantaInventario';
                """);
            migrationBuilder.Sql("""
                UPDATE "ProductionOrders"
                SET "MaterialsStatus" = 'PendienteRevisionBodega'
                WHERE "MaterialsStatus" = 'PendienteRevisionPlantaInventario';
                """);
            migrationBuilder.Sql("""
                UPDATE "Users" SET "Rol" = 'Bodeguero' WHERE "Rol" = 'Encargado de bodega';
                """);
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario" SET "Nombre" = 'Bodega 1'
                WHERE "Id" = 1 AND "Nombre" = 'Planta de Inventario 1';
                """);
            migrationBuilder.Sql("""
                UPDATE "PlantasInventario" SET "Nombre" = 'Bodega 2'
                WHERE "Id" = 2 AND "Nombre" = 'Planta de Inventario 2';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_EntregasMaterial_Users_EncargadoDeBodegaId",
                table: "EntregasMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_PlantasInventario_PlantaInventarioId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesMaterial_PlantasInventario_PlantaInventarioId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPlantasInventario_PlantasInventario_PlantaInventarioId",
                table: "UserPlantasInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPlantasInventario_Users_UserId",
                table: "UserPlantasInventario");

            RenamePrimaryKeysForPostgres(migrationBuilder, up: false);

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "UserPlantasInventario",
                newName: "BodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_UserPlantasInventario_PlantaInventarioId",
                table: "UserPlantasInventario",
                newName: "IX_UserBodegas_BodegaId");

            migrationBuilder.RenameTable(
                name: "UserPlantasInventario",
                newName: "UserBodegas");

            migrationBuilder.RenameTable(
                name: "PlantasInventario",
                newName: "Bodegas");

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "SolicitudesMaterial",
                newName: "BodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_SolicitudesMaterial_PlantaInventarioId",
                table: "SolicitudesMaterial",
                newName: "IX_SolicitudesMaterial_BodegaId");

            migrationBuilder.RenameColumn(
                name: "PlantaInventarioId",
                table: "Materials",
                newName: "BodegaId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_PlantaInventarioId",
                table: "Materials",
                newName: "IX_Materials_BodegaId");

            migrationBuilder.RenameColumn(
                name: "NumeroGrupo",
                table: "Fichas",
                newName: "FichaCode");

            migrationBuilder.RenameColumn(
                name: "EncargadoDeBodegaId",
                table: "EntregasMaterial",
                newName: "BodegueroId");

            migrationBuilder.RenameIndex(
                name: "IX_EntregasMaterial_EncargadoDeBodegaId",
                table: "EntregasMaterial",
                newName: "IX_EntregasMaterial_BodegueroId");

            migrationBuilder.AddForeignKey(
                name: "FK_EntregasMaterial_Users_BodegueroId",
                table: "EntregasMaterial",
                column: "BodegueroId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Bodegas_BodegaId",
                table: "Materials",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesMaterial_Bodegas_BodegaId",
                table: "SolicitudesMaterial",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBodegas_Bodegas_BodegaId",
                table: "UserBodegas",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBodegas_Users_UserId",
                table: "UserBodegas",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        // SQLite ignora nombres de PK; PostgreSQL los conserva al RENAME TABLE.
        private static void RenamePrimaryKeysForPostgres(MigrationBuilder migrationBuilder, bool up)
        {
            if (!string.Equals(
                    migrationBuilder.ActiveProvider,
                    "Npgsql.EntityFrameworkCore.PostgreSQL",
                    StringComparison.Ordinal))
                return;

            if (up)
            {
                migrationBuilder.Sql("""ALTER TABLE "PlantasInventario" RENAME CONSTRAINT "PK_Bodegas" TO "PK_PlantasInventario";""");
                migrationBuilder.Sql("""ALTER TABLE "UserPlantasInventario" RENAME CONSTRAINT "PK_UserBodegas" TO "PK_UserPlantasInventario";""");
            }
            else
            {
                migrationBuilder.Sql("""ALTER TABLE "PlantasInventario" RENAME CONSTRAINT "PK_PlantasInventario" TO "PK_Bodegas";""");
                migrationBuilder.Sql("""ALTER TABLE "UserPlantasInventario" RENAME CONSTRAINT "PK_UserPlantasInventario" TO "PK_UserBodegas";""");
            }
        }
    }
}

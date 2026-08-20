using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBodegas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Crear la puente mientras Users.BodegaId todavía existe.
            migrationBuilder.CreateTable(
                name: "UserBodegas",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    BodegaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBodegas", x => new { x.UserId, x.BodegaId });
                    table.ForeignKey(
                        name: "FK_UserBodegas_Bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalTable: "Bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBodegas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBodegas_BodegaId",
                table: "UserBodegas",
                column: "BodegaId");

            // 2) Copiar asignaciones existentes (un User.BodegaId → una fila). SQL portable SQLite/PostgreSQL.
            migrationBuilder.Sql("""
                INSERT INTO "UserBodegas" ("UserId", "BodegaId")
                SELECT "Id", "BodegaId" FROM "Users"
                WHERE "BodegaId" IS NOT NULL;
                """);

            // 3) Quitar la FK singular solo después de migrar los datos.
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Bodegas_BodegaId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BodegaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BodegaId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            // Down lossy: si había varias bodegas, queda la de menor Id.
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "BodegaId" = (
                    SELECT MIN(ub."BodegaId") FROM "UserBodegas" ub WHERE ub."UserId" = "Users"."Id"
                )
                WHERE EXISTS (
                    SELECT 1 FROM "UserBodegas" ub WHERE ub."UserId" = "Users"."Id"
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BodegaId",
                table: "Users",
                column: "BodegaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Bodegas_BodegaId",
                table: "Users",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "UserBodegas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFichaTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegisteredByUserId",
                table: "ProductionSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorUserId",
                table: "Fichas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Turno",
                table: "Fichas",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_RegisteredByUserId",
                table: "ProductionSessions",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_InstructorUserId",
                table: "Fichas",
                column: "InstructorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fichas_Users_InstructorUserId",
                table: "Fichas",
                column: "InstructorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSessions_Users_RegisteredByUserId",
                table: "ProductionSessions",
                column: "RegisteredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fichas_Users_InstructorUserId",
                table: "Fichas");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSessions_Users_RegisteredByUserId",
                table: "ProductionSessions");

            migrationBuilder.DropIndex(
                name: "IX_ProductionSessions_RegisteredByUserId",
                table: "ProductionSessions");

            migrationBuilder.DropIndex(
                name: "IX_Fichas_InstructorUserId",
                table: "Fichas");

            migrationBuilder.DropColumn(
                name: "RegisteredByUserId",
                table: "ProductionSessions");

            migrationBuilder.DropColumn(
                name: "InstructorUserId",
                table: "Fichas");

            migrationBuilder.DropColumn(
                name: "Turno",
                table: "Fichas");
        }
    }
}

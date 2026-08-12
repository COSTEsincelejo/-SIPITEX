using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRequestSolicitante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SolicitanteId",
                table: "MaterialRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_SolicitanteId",
                table: "MaterialRequests",
                column: "SolicitanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_Users_SolicitanteId",
                table: "MaterialRequests",
                column: "SolicitanteId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequests_Users_SolicitanteId",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_SolicitanteId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "SolicitanteId",
                table: "MaterialRequests");
        }
    }
}

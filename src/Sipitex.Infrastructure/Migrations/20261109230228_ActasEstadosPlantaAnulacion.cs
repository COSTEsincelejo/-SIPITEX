using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActasEstadosPlantaAnulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnuladaPorUserId",
                table: "ActasMovimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnuladaUtc",
                table: "ActasMovimiento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "ActasMovimiento",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "ActasMovimiento",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantaDestinoId",
                table: "ActasMovimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantaOrigenId",
                table: "ActasMovimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_AnuladaPorUserId",
                table: "ActasMovimiento",
                column: "AnuladaPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_PlantaDestinoId",
                table: "ActasMovimiento",
                column: "PlantaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_PlantaOrigenId",
                table: "ActasMovimiento",
                column: "PlantaOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimiento_PlantasInventario_PlantaDestinoId",
                table: "ActasMovimiento",
                column: "PlantaDestinoId",
                principalTable: "PlantasInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimiento_PlantasInventario_PlantaOrigenId",
                table: "ActasMovimiento",
                column: "PlantaOrigenId",
                principalTable: "PlantasInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimiento_Users_AnuladaPorUserId",
                table: "ActasMovimiento",
                column: "AnuladaPorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActasMovimiento_PlantasInventario_PlantaDestinoId",
                table: "ActasMovimiento");

            migrationBuilder.DropForeignKey(
                name: "FK_ActasMovimiento_PlantasInventario_PlantaOrigenId",
                table: "ActasMovimiento");

            migrationBuilder.DropForeignKey(
                name: "FK_ActasMovimiento_Users_AnuladaPorUserId",
                table: "ActasMovimiento");

            migrationBuilder.DropIndex(
                name: "IX_ActasMovimiento_AnuladaPorUserId",
                table: "ActasMovimiento");

            migrationBuilder.DropIndex(
                name: "IX_ActasMovimiento_PlantaDestinoId",
                table: "ActasMovimiento");

            migrationBuilder.DropIndex(
                name: "IX_ActasMovimiento_PlantaOrigenId",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "AnuladaPorUserId",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "AnuladaUtc",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "PlantaDestinoId",
                table: "ActasMovimiento");

            migrationBuilder.DropColumn(
                name: "PlantaOrigenId",
                table: "ActasMovimiento");
        }
    }
}

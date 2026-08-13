using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudMaterialInsumosLibres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FichaId",
                table: "SolicitudesMaterial",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "DescripcionLibre",
                table: "SolicitudesMaterial",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionOrderId",
                table: "SolicitudesMaterial",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "SolicitudesMaterial",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "PorFicha");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "DetallesSolicitudMaterial",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "DescripcionItem",
                table: "DetallesSolicitudMaterial",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_ProductionOrderId",
                table: "SolicitudesMaterial",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Tipo",
                table: "SolicitudesMaterial",
                column: "Tipo");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesMaterial_ProductionOrders_ProductionOrderId",
                table: "SolicitudesMaterial",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesMaterial_ProductionOrders_ProductionOrderId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesMaterial_ProductionOrderId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesMaterial_Tipo",
                table: "SolicitudesMaterial");

            migrationBuilder.DropColumn(
                name: "DescripcionLibre",
                table: "SolicitudesMaterial");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "SolicitudesMaterial");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "SolicitudesMaterial");

            migrationBuilder.DropColumn(
                name: "DescripcionItem",
                table: "DetallesSolicitudMaterial");

            migrationBuilder.AlterColumn<int>(
                name: "FichaId",
                table: "SolicitudesMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "DetallesSolicitudMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}

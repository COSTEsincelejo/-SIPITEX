using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderMesFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "ProductionOrders",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStageId",
                table: "ProductionOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinishedGoodMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    StageId = table.Column<int>(type: "INTEGER", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Observations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodMovements_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinishedGoodMovements_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Stock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstructorStagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    StageName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorStagePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorStagePermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductFlowTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFlowTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActorUserName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    StageId = table.Column<int>(type: "INTEGER", nullable: true),
                    StageName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderHistoryEntries_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    InstructorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Observations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    QuantityReceived = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantitySent = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityWithdrawn = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStages_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStages_Users_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductFlowStageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductFlowTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFlowStageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFlowStageTemplates_ProductFlowTemplates_ProductFlowTemplateId",
                        column: x => x.ProductFlowTemplateId,
                        principalTable: "ProductFlowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderStageMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStageId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStageId = table.Column<int>(type: "INTEGER", nullable: true),
                    MovementType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorizedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Motive = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Observations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderStageMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrderStages_FromStageId",
                        column: x => x.FromStageId,
                        principalTable: "ProductionOrderStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrderStages_ToStageId",
                        column: x => x.ToStageId,
                        principalTable: "ProductionOrderStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_Users_AuthorizedByUserId",
                        column: x => x.AuthorizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CurrentStageId",
                table: "ProductionOrders",
                column: "CurrentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodMovements_ActorUserId",
                table: "FinishedGoodMovements",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodMovements_ProductionOrderId",
                table: "FinishedGoodMovements",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodMovements_ProductName",
                table: "FinishedGoodMovements",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodStocks_ProductName",
                table: "FinishedGoodStocks",
                column: "ProductName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorStagePermissions_UserId_StageName",
                table: "InstructorStagePermissions",
                columns: new[] { "UserId", "StageName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFlowStageTemplates_ProductFlowTemplateId_SortOrder",
                table: "ProductFlowStageTemplates",
                columns: new[] { "ProductFlowTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFlowTemplates_ProductName",
                table: "ProductFlowTemplates",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderHistoryEntries_ProductionOrderId_AtUtc",
                table: "ProductionOrderHistoryEntries",
                columns: new[] { "ProductionOrderId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageMovements_ActorUserId",
                table: "ProductionOrderStageMovements",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageMovements_AuthorizedByUserId",
                table: "ProductionOrderStageMovements",
                column: "AuthorizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageMovements_FromStageId",
                table: "ProductionOrderStageMovements",
                column: "FromStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageMovements_ProductionOrderId",
                table: "ProductionOrderStageMovements",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageMovements_ToStageId",
                table: "ProductionOrderStageMovements",
                column: "ToStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStages_InstructorUserId",
                table: "ProductionOrderStages",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStages_ProductionOrderId_SortOrder",
                table: "ProductionOrderStages",
                columns: new[] { "ProductionOrderId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_ProductionOrderStages_CurrentStageId",
                table: "ProductionOrders",
                column: "CurrentStageId",
                principalTable: "ProductionOrderStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_ProductionOrderStages_CurrentStageId",
                table: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "FinishedGoodMovements");

            migrationBuilder.DropTable(
                name: "FinishedGoodStocks");

            migrationBuilder.DropTable(
                name: "InstructorStagePermissions");

            migrationBuilder.DropTable(
                name: "ProductFlowStageTemplates");

            migrationBuilder.DropTable(
                name: "ProductionOrderHistoryEntries");

            migrationBuilder.DropTable(
                name: "ProductionOrderStageMovements");

            migrationBuilder.DropTable(
                name: "ProductFlowTemplates");

            migrationBuilder.DropTable(
                name: "ProductionOrderStages");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_CurrentStageId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CurrentStageId",
                table: "ProductionOrders");
        }
    }
}

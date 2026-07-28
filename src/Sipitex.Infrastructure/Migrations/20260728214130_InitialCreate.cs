using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FunctionalRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Observation = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false),
                    Stock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MinStock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LastEntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NonFunctionalRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Observation = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonFunctionalRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TotalQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ProducedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Deadline = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fichas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FichaCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", nullable: false),
                    InstructorName = table.Column<string>(type: "TEXT", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fichas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fichas_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitsInspected = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoReproceso = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Responsable = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    InspectionDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityRecords_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FichaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Units = table.Column<int>(type: "INTEGER", nullable: false),
                    Observations = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSessions_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionSessions_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FichaAsignadaId = table.Column<int>(type: "INTEGER", nullable: true),
                    PermisosExtendidos = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Fichas_FichaAsignadaId",
                        column: x => x.FichaAsignadaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AlertDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlertType = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertDeliveries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlertType = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveries_UserId",
                table: "AlertDeliveries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPreferences_UserId_AlertType",
                table: "AlertPreferences",
                columns: new[] { "UserId", "AlertType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_MaterialId",
                table: "BomItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_ProductionOrderId",
                table: "Fichas",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalRequirements_Code",
                table: "FunctionalRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_MaterialId",
                table: "MaterialRequests",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_ProductionOrderId",
                table: "MaterialRequests",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_NonFunctionalRequirements_Code",
                table: "NonFunctionalRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                table: "ProductionOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_FichaId",
                table: "ProductionSessions",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_ProductionOrderId",
                table: "ProductionSessions",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityRecords_ProductionOrderId",
                table: "QualityRecords",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FichaAsignadaId",
                table: "Users",
                column: "FichaAsignadaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertDeliveries");

            migrationBuilder.DropTable(
                name: "AlertPreferences");

            migrationBuilder.DropTable(
                name: "BomItems");

            migrationBuilder.DropTable(
                name: "FunctionalRequirements");

            migrationBuilder.DropTable(
                name: "MaterialRequests");

            migrationBuilder.DropTable(
                name: "NonFunctionalRequirements");

            migrationBuilder.DropTable(
                name: "ProductionSessions");

            migrationBuilder.DropTable(
                name: "QualityRecords");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Fichas");

            migrationBuilder.DropTable(
                name: "ProductionOrders");
        }
    }
}

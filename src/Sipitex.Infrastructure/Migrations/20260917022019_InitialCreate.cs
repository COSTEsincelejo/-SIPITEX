using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Entity = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "BomProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsReference = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HabilitadoParaOrdenes = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Linea = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TallaInicial = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TipoEmpaque = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DescripcionPrenda = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FechaSolicitud = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaElaboracion = table.Column<DateOnly>(type: "date", nullable: true),
                    AnioMuestrario = table.Column<int>(type: "integer", nullable: true),
                    EsDisenoNuevo = table.Column<bool>(type: "boolean", nullable: false),
                    EsReplica = table.Column<bool>(type: "boolean", nullable: false),
                    EsBancoDeMuestras = table.Column<bool>(type: "boolean", nullable: false),
                    Disenador = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Patronista = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Digitacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Stock = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionalRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NonFunctionalRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonFunctionalRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductFlowTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFlowTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomProductMedidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomProductId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tolerancia = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ComoMedir = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductMedidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductMedidas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomProductPiezas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomProductId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Tela = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductPiezas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductPiezas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomProductTallas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomProductId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductTallas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductTallas_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    Stock = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinStock = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastEntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CostoAdquisicion = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PlantaInventarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_PlantasInventario_PlantaInventarioId",
                        column: x => x.PlantaInventarioId,
                        principalTable: "PlantasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductFlowStageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductFlowTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFlowStageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFlowStageTemplates_ProductFlowTemplates_ProductFlowT~",
                        column: x => x.ProductFlowTemplateId,
                        principalTable: "ProductFlowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomProductMedidaValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomProductMedidaId = table.Column<int>(type: "integer", nullable: false),
                    BomProductTallaId = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductMedidaValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomProductMedidaValores_BomProductMedidas_BomProductMedidaId",
                        column: x => x.BomProductMedidaId,
                        principalTable: "BomProductMedidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomProductMedidaValores_BomProductTallas_BomProductTallaId",
                        column: x => x.BomProductTallaId,
                        principalTable: "BomProductTallas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomItems_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActasMovimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: true),
                    EstadoProductoOrigen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EstadoProductoDestino = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EntregaNombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntregaCargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntregaConformidadUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntregaFirmaPng = table.Column<byte[]>(type: "bytea", nullable: true),
                    RecibeNombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RecibeCargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RecibeConformidadUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecibeFirmaPng = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreadoPorUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasMovimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActasMovimientoDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActaMovimientoId = table.Column<int>(type: "integer", nullable: false),
                    ItemTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    ConsumoMaterialId = table.Column<int>(type: "integer", nullable: true),
                    StockMovementId = table.Column<int>(type: "integer", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActasMovimientoDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_ActasMovimiento_ActaMovimientoId",
                        column: x => x.ActaMovimientoId,
                        principalTable: "ActasMovimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActasMovimientoDetalle_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomProductInstructors",
                columns: table => new
                {
                    BomProductId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomProductInstructors", x => new { x.BomProductId, x.UserId });
                    table.ForeignKey(
                        name: "FK_BomProductInstructors_BomProducts_BomProductId",
                        column: x => x.BomProductId,
                        principalTable: "BomProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumosMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    GrupoConfeccionId = table.Column<int>(type: "integer", nullable: true),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponsableUserId = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterial_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesSolicitudMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudMaterialId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    DescripcionItem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CantidadSolicitada = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CantidadAprobada = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EstadoItem = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesSolicitudMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesSolicitudMaterial_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntregasMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SolicitudMaterialId = table.Column<int>(type: "integer", nullable: false),
                    EncargadoDeBodegaId = table.Column<int>(type: "integer", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasMaterial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FichaInstructors",
                columns: table => new
                {
                    FichaId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Proceso = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaInstructors", x => new { x.FichaId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Fichas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroGrupo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProcessName = table.Column<string>(type: "text", nullable: false),
                    InstructorName = table.Column<string>(type: "text", nullable: false),
                    Turno = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InstructorUserId = table.Column<int>(type: "integer", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: true),
                    AssignedOrderText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fichas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Rol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FichaAsignadaId = table.Column<int>(type: "integer", nullable: true),
                    PermisosExtendidos = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhotoPath = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    FuncionDescripcion = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "InstructorStagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StageName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
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
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Origen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StockResultante = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPlantasInventario",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlantaInventarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlantasInventario", x => new { x.UserId, x.PlantaInventarioId });
                    table.ForeignKey(
                        name: "FK_UserPlantasInventario_PlantasInventario_PlantaInventarioId",
                        column: x => x.PlantaInventarioId,
                        principalTable: "PlantasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPlantasInventario_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: true),
                    StageId = table.Column<int>(type: "integer", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodMovements_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GruposConfeccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    InstructorUserId = table.Column<int>(type: "integer", nullable: false),
                    FechaRealizacion = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CantidadPrendas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposConfeccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposConfeccion_Users_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    SolicitanteId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "FK_MaterialRequests_Users_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Campo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ValorAnterior = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValorNuevo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderChangeLogs_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrendasTrazables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Talla = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUserId = table.Column<int>(type: "integer", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrendasTrazables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrendasTrazables_Users_CreadoPorUserId",
                        column: x => x.CreadoPorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderBomSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderBomSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderBomSnapshots_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActorUserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StageId = table.Column<int>(type: "integer", nullable: true),
                    StageName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderHistoryEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderMaterialRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityDelivered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    Observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderMaterialRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderMaterialRequirements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ClientName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    ProducedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EstadoProducto = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaterialsStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CurrentStageId = table.Column<int>(type: "integer", nullable: true),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InstructorUserId = table.Column<int>(type: "integer", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    QuantityProcessed = table.Column<int>(type: "integer", nullable: false),
                    QuantitySent = table.Column<int>(type: "integer", nullable: false),
                    QuantityWithdrawn = table.Column<int>(type: "integer", nullable: false)
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
                name: "ProductionSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FichaId = table.Column<int>(type: "integer", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<int>(type: "integer", nullable: false),
                    Observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredByUserId = table.Column<int>(type: "integer", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ProductionSessions_Users_RegisteredByUserId",
                        column: x => x.RegisteredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QualityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    UnitsInspected = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Clasificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoReproceso = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Responsable = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    InspectionDate = table.Column<DateOnly>(type: "date", nullable: false)
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
                name: "SolicitudesMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PorFicha"),
                    FichaId = table.Column<int>(type: "integer", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: true),
                    DescripcionLibre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SolicitanteId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResueltoPorId = table.Column<int>(type: "integer", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PlantaInventarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_PlantasInventario_PlantaInventarioId",
                        column: x => x.PlantaInventarioId,
                        principalTable: "PlantasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Users_ResueltoPorId",
                        column: x => x.ResueltoPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SolicitudesMaterial_Users_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderStageMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionOrderId = table.Column<int>(type: "integer", nullable: false),
                    FromStageId = table.Column<int>(type: "integer", nullable: true),
                    ToStageId = table.Column<int>(type: "integer", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    AuthorizedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Motive = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderStageMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrderStages_FromSta~",
                        column: x => x.FromStageId,
                        principalTable: "ProductionOrderStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrderStages_ToStage~",
                        column: x => x.ToStageId,
                        principalTable: "ProductionOrderStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageMovements_ProductionOrders_ProductionOr~",
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

            migrationBuilder.InsertData(
                table: "PlantasInventario",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Planta de Inventario 1" },
                    { 2, true, "Planta de Inventario 2" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_CreadoPorUserId",
                table: "ActasMovimiento",
                column: "CreadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_FechaUtc",
                table: "ActasMovimiento",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_Numero",
                table: "ActasMovimiento",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimiento_ProductionOrderId",
                table: "ActasMovimiento",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ActaMovimientoId",
                table: "ActasMovimientoDetalle",
                column: "ActaMovimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ConsumoMaterialId",
                table: "ActasMovimientoDetalle",
                column: "ConsumoMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_MaterialId",
                table: "ActasMovimientoDetalle",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_ProductionOrderId",
                table: "ActasMovimientoDetalle",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ActasMovimientoDetalle_StockMovementId",
                table: "ActasMovimientoDetalle",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Action",
                table: "ActivityLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");

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
                name: "IX_BomItems_BomProductId",
                table: "BomItems",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_MaterialId",
                table: "BomItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductInstructors_UserId",
                table: "BomProductInstructors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidas_BomProductId",
                table: "BomProductMedidas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidas_BomProductId_Tipo_Orden",
                table: "BomProductMedidas",
                columns: new[] { "BomProductId", "Tipo", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidaValores_BomProductMedidaId_BomProductTallaId",
                table: "BomProductMedidaValores",
                columns: new[] { "BomProductMedidaId", "BomProductTallaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomProductMedidaValores_BomProductTallaId",
                table: "BomProductMedidaValores",
                column: "BomProductTallaId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductPiezas_BomProductId",
                table: "BomProductPiezas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductPiezas_BomProductId_Orden",
                table: "BomProductPiezas",
                columns: new[] { "BomProductId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_BomProducts_Codigo",
                table: "BomProducts",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomProducts_ProductName",
                table: "BomProducts",
                column: "ProductName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomProductTallas_BomProductId",
                table: "BomProductTallas",
                column: "BomProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomProductTallas_BomProductId_Orden",
                table: "BomProductTallas",
                columns: new[] { "BomProductId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_FechaUtc",
                table: "ConsumosMaterial",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_GrupoConfeccionId",
                table: "ConsumosMaterial",
                column: "GrupoConfeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_MaterialId",
                table: "ConsumosMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_ProductionOrderId",
                table: "ConsumosMaterial",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterial_ResponsableUserId",
                table: "ConsumosMaterial",
                column: "ResponsableUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSolicitudMaterial_MaterialId",
                table: "DetallesSolicitudMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSolicitudMaterial_SolicitudMaterialId",
                table: "DetallesSolicitudMaterial",
                column: "SolicitudMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_Codigo",
                table: "EntregasMaterial",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_EncargadoDeBodegaId",
                table: "EntregasMaterial",
                column: "EncargadoDeBodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasMaterial_SolicitudMaterialId",
                table: "EntregasMaterial",
                column: "SolicitudMaterialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichaInstructors_UserId",
                table: "FichaInstructors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_InstructorUserId",
                table: "Fichas",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_ProductionOrderId",
                table: "Fichas",
                column: "ProductionOrderId");

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
                name: "IX_FunctionalRequirements_Code",
                table: "FunctionalRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_FechaRealizacion",
                table: "GruposConfeccion",
                column: "FechaRealizacion");

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_InstructorUserId",
                table: "GruposConfeccion",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposConfeccion_ProductionOrderId",
                table: "GruposConfeccion",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorStagePermissions_UserId_StageName",
                table: "InstructorStagePermissions",
                columns: new[] { "UserId", "StageName" },
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
                name: "IX_MaterialRequests_SolicitanteId",
                table: "MaterialRequests",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_PlantaInventarioId",
                table: "Materials",
                column: "PlantaInventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NonFunctionalRequirements_Code",
                table: "NonFunctionalRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_FechaUtc",
                table: "OrderChangeLogs",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_ProductionOrderId",
                table: "OrderChangeLogs",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogs_UsuarioId",
                table: "OrderChangeLogs",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId_CreatedAtUtc",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_Codigo",
                table: "PrendasTrazables",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_CreadoPorUserId",
                table: "PrendasTrazables",
                column: "CreadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_CreadoUtc",
                table: "PrendasTrazables",
                column: "CreadoUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PrendasTrazables_ProductionOrderId",
                table: "PrendasTrazables",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFlowStageTemplates_ProductFlowTemplateId_SortOrder",
                table: "ProductFlowStageTemplates",
                columns: new[] { "ProductFlowTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFlowTemplates_ProductName",
                table: "ProductFlowTemplates",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderBomSnapshots_MaterialId",
                table: "ProductionOrderBomSnapshots",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderBomSnapshots_ProductionOrderId",
                table: "ProductionOrderBomSnapshots",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderHistoryEntries_ProductionOrderId_AtUtc",
                table: "ProductionOrderHistoryEntries",
                columns: new[] { "ProductionOrderId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_MaterialId",
                table: "ProductionOrderMaterialRequirements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_ProductionOrderId",
                table: "ProductionOrderMaterialRequirements",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderMaterialRequirements_ProductionOrderId_Mater~",
                table: "ProductionOrderMaterialRequirements",
                columns: new[] { "ProductionOrderId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CurrentStageId",
                table: "ProductionOrders",
                column: "CurrentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                table: "ProductionOrders",
                column: "OrderNumber",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_FichaId",
                table: "ProductionSessions",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_ProductionOrderId",
                table: "ProductionSessions",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_RegisteredByUserId",
                table: "ProductionSessions",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityRecords_ProductionOrderId",
                table: "QualityRecords",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Codigo",
                table: "SolicitudesMaterial",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Estado",
                table: "SolicitudesMaterial",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_FichaId",
                table: "SolicitudesMaterial",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_PlantaInventarioId",
                table: "SolicitudesMaterial",
                column: "PlantaInventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_ProductionOrderId",
                table: "SolicitudesMaterial",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_ResueltoPorId",
                table: "SolicitudesMaterial",
                column: "ResueltoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_SolicitanteId",
                table: "SolicitudesMaterial",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesMaterial_Tipo",
                table: "SolicitudesMaterial",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_FechaUtc",
                table: "StockMovements",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MaterialId",
                table: "StockMovements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_UsuarioId",
                table: "StockMovements",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlantasInventario_PlantaInventarioId",
                table: "UserPlantasInventario",
                column: "PlantaInventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FichaAsignadaId",
                table: "Users",
                column: "FichaAsignadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimiento_ProductionOrders_ProductionOrderId",
                table: "ActasMovimiento",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimiento_Users_CreadoPorUserId",
                table: "ActasMovimiento",
                column: "CreadoPorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimientoDetalle_ConsumosMaterial_ConsumoMaterialId",
                table: "ActasMovimientoDetalle",
                column: "ConsumoMaterialId",
                principalTable: "ConsumosMaterial",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimientoDetalle_ProductionOrders_ProductionOrderId",
                table: "ActasMovimientoDetalle",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActasMovimientoDetalle_StockMovements_StockMovementId",
                table: "ActasMovimientoDetalle",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertDeliveries_Users_UserId",
                table: "AlertDeliveries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertPreferences_Users_UserId",
                table: "AlertPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BomProductInstructors_Users_UserId",
                table: "BomProductInstructors",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumosMaterial_GruposConfeccion_GrupoConfeccionId",
                table: "ConsumosMaterial",
                column: "GrupoConfeccionId",
                principalTable: "GruposConfeccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumosMaterial_ProductionOrders_ProductionOrderId",
                table: "ConsumosMaterial",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumosMaterial_Users_ResponsableUserId",
                table: "ConsumosMaterial",
                column: "ResponsableUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesSolicitudMaterial_SolicitudesMaterial_SolicitudMate~",
                table: "DetallesSolicitudMaterial",
                column: "SolicitudMaterialId",
                principalTable: "SolicitudesMaterial",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntregasMaterial_SolicitudesMaterial_SolicitudMaterialId",
                table: "EntregasMaterial",
                column: "SolicitudMaterialId",
                principalTable: "SolicitudesMaterial",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntregasMaterial_Users_EncargadoDeBodegaId",
                table: "EntregasMaterial",
                column: "EncargadoDeBodegaId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FichaInstructors_Fichas_FichaId",
                table: "FichaInstructors",
                column: "FichaId",
                principalTable: "Fichas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FichaInstructors_Users_UserId",
                table: "FichaInstructors",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fichas_ProductionOrders_ProductionOrderId",
                table: "Fichas",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fichas_Users_InstructorUserId",
                table: "Fichas",
                column: "InstructorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodMovements_ProductionOrders_ProductionOrderId",
                table: "FinishedGoodMovements",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GruposConfeccion_ProductionOrders_ProductionOrderId",
                table: "GruposConfeccion",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_ProductionOrders_ProductionOrderId",
                table: "MaterialRequests",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderChangeLogs_ProductionOrders_ProductionOrderId",
                table: "OrderChangeLogs",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrendasTrazables_ProductionOrders_ProductionOrderId",
                table: "PrendasTrazables",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderBomSnapshots_ProductionOrders_ProductionOrde~",
                table: "ProductionOrderBomSnapshots",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderHistoryEntries_ProductionOrders_ProductionOr~",
                table: "ProductionOrderHistoryEntries",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderMaterialRequirements_ProductionOrders_Produc~",
                table: "ProductionOrderMaterialRequirements",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_Fichas_ProductionOrders_ProductionOrderId",
                table: "Fichas");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderStages_ProductionOrders_ProductionOrderId",
                table: "ProductionOrderStages");

            migrationBuilder.DropForeignKey(
                name: "FK_Fichas_Users_InstructorUserId",
                table: "Fichas");

            migrationBuilder.DropTable(
                name: "ActasMovimientoDetalle");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AlertDeliveries");

            migrationBuilder.DropTable(
                name: "AlertPreferences");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "BomItems");

            migrationBuilder.DropTable(
                name: "BomProductInstructors");

            migrationBuilder.DropTable(
                name: "BomProductMedidaValores");

            migrationBuilder.DropTable(
                name: "BomProductPiezas");

            migrationBuilder.DropTable(
                name: "DetallesSolicitudMaterial");

            migrationBuilder.DropTable(
                name: "EntregasMaterial");

            migrationBuilder.DropTable(
                name: "FichaInstructors");

            migrationBuilder.DropTable(
                name: "FinishedGoodMovements");

            migrationBuilder.DropTable(
                name: "FinishedGoodStocks");

            migrationBuilder.DropTable(
                name: "FunctionalRequirements");

            migrationBuilder.DropTable(
                name: "InstructorStagePermissions");

            migrationBuilder.DropTable(
                name: "MaterialRequests");

            migrationBuilder.DropTable(
                name: "NonFunctionalRequirements");

            migrationBuilder.DropTable(
                name: "OrderChangeLogs");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PrendasTrazables");

            migrationBuilder.DropTable(
                name: "ProductFlowStageTemplates");

            migrationBuilder.DropTable(
                name: "ProductionOrderBomSnapshots");

            migrationBuilder.DropTable(
                name: "ProductionOrderHistoryEntries");

            migrationBuilder.DropTable(
                name: "ProductionOrderMaterialRequirements");

            migrationBuilder.DropTable(
                name: "ProductionOrderStageMovements");

            migrationBuilder.DropTable(
                name: "ProductionSessions");

            migrationBuilder.DropTable(
                name: "QualityRecords");

            migrationBuilder.DropTable(
                name: "UserPlantasInventario");

            migrationBuilder.DropTable(
                name: "ActasMovimiento");

            migrationBuilder.DropTable(
                name: "ConsumosMaterial");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "BomProductMedidas");

            migrationBuilder.DropTable(
                name: "BomProductTallas");

            migrationBuilder.DropTable(
                name: "SolicitudesMaterial");

            migrationBuilder.DropTable(
                name: "ProductFlowTemplates");

            migrationBuilder.DropTable(
                name: "GruposConfeccion");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "BomProducts");

            migrationBuilder.DropTable(
                name: "PlantasInventario");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "ProductionOrderStages");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Fichas");
        }
    }
}

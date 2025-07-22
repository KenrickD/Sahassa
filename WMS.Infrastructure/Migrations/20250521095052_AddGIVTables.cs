using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGIVTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_GIV_AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangesDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_AuditLog_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_Pallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Pallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Pallet_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Pallet_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiresLotTracking = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresExpirationDate = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresSerialNumber = table.Column<bool>(type: "boolean", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsHazardous = table.Column<bool>(type: "boolean", nullable: false),
                    IsFragile = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Product_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Product_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransportTypeID = table.Column<int>(type: "integer", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualReceivingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnstuffingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualUnstuffingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PONumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Inventory_TB_GIV_Pallet_PalletId",
                        column: x => x.PalletId,
                        principalTable: "TB_GIV_Pallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Inventory_TB_GIV_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TB_GIV_Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Inventory_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Inventory_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_InventoryMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_InventoryMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_GIV_Inventory_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "TB_GIV_Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Location_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Location_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_AuditLog_WarehouseId",
                table: "TB_GIV_AuditLog",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Inventory_LocationId",
                table: "TB_GIV_Inventory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Inventory_PalletId",
                table: "TB_GIV_Inventory",
                column: "PalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Inventory_ProductId",
                table: "TB_GIV_Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Inventory_WarehouseId",
                table: "TB_GIV_Inventory",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_FromLocationId",
                table: "TB_GIV_InventoryMovement",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_InventoryId",
                table: "TB_GIV_InventoryMovement",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_ToLocationId",
                table: "TB_GIV_InventoryMovement",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_WarehouseId",
                table: "TB_GIV_InventoryMovement",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Pallet_LocationId",
                table: "TB_GIV_Pallet",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Pallet_WarehouseId",
                table: "TB_GIV_Pallet",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Product_ClientId",
                table: "TB_GIV_Product",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Product_WarehouseId",
                table: "TB_GIV_Product",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_AuditLog");

            migrationBuilder.DropTable(
                name: "TB_GIV_InventoryMovement");

            migrationBuilder.DropTable(
                name: "TB_GIV_Inventory");

            migrationBuilder.DropTable(
                name: "TB_GIV_Pallet");

            migrationBuilder.DropTable(
                name: "TB_GIV_Product");
        }
    }
}

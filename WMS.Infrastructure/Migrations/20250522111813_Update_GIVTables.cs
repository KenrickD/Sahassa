using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_GIVTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_InventoryMovement_TB_GIV_Inventory_InventoryId",
                table: "TB_GIV_InventoryMovement");

            migrationBuilder.DropTable(
                name: "TB_GIV_Inventory");

            migrationBuilder.DropTable(
                name: "TB_GIV_Pallet");

            migrationBuilder.DropTable(
                name: "TB_GIV_Product");

            migrationBuilder.DropColumn(
                name: "JobNo_GW",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "UnstuffedBy",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "UnstuffedDate",
                table: "TB_GIV_Container");

            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "TB_GIV_InventoryMovement",
                newName: "FinishedGoodId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_InventoryMovement_InventoryId",
                table: "TB_GIV_InventoryMovement",
                newName: "IX_TB_GIV_InventoryMovement_FinishedGoodId");

            migrationBuilder.AddColumn<Guid>(
                name: "FG_ID",
                table: "TB_GIV_InventoryMovement",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "ContainerNo_GW",
                table: "TB_GIV_Container",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "TB_GIV_FinishedGood",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SKU = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BatchNo = table.Column<string>(type: "text", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FinishedGood", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FinishedGood_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RawMaterial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialNo = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RawMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RawMaterial_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FGInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FG_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageLocation_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletCode = table.Column<int>(type: "integer", maxLength: 11, nullable: true),
                    HandledBy = table.Column<string>(type: "text", nullable: true),
                    StoredBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FGInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_Receive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeID = table.Column<int>(type: "integer", nullable: false),
                    Material_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    RawMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Container_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNo = table.Column<string>(type: "text", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_Receive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "TB_GIV_Container",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_GIV_RawMaterial_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "TB_GIV_RawMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceiveItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RM_Receive_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletCode = table.Column<int>(type: "integer", maxLength: 11, nullable: false),
                    HandledBy = table.Column<string>(type: "text", nullable: false),
                    StorageLocation_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReceiveItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceiveItem_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                        column: x => x.GIV_RM_ReceiveId,
                        principalTable: "TB_GIV_RM_Receive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceiveItem_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceiveItem_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceiveItemBreakdown",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RM_ReceiveItem_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReceiveItemBreakdown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceiveItem_GIV_RM~",
                        column: x => x.GIV_RM_ReceiveItemId,
                        principalTable: "TB_GIV_RM_ReceiveItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_FinishedGoodId",
                table: "TB_GIV_FGInventory",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_LocationId",
                table: "TB_GIV_FGInventory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_WarehouseId",
                table: "TB_GIV_FGInventory",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FinishedGood_WarehouseId",
                table: "TB_GIV_FinishedGood",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RawMaterial_WarehouseId",
                table: "TB_GIV_RawMaterial",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_RawMaterialId",
                table: "TB_GIV_RM_Receive",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_WarehouseId",
                table: "TB_GIV_RM_Receive",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_LocationId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_WarehouseId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceiveItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_WarehouseId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_InventoryMovement_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_InventoryMovement",
                column: "FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_InventoryMovement_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_InventoryMovement");

            migrationBuilder.DropTable(
                name: "TB_GIV_FGInventory");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropTable(
                name: "TB_GIV_FinishedGood");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_Receive");

            migrationBuilder.DropTable(
                name: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "FG_ID",
                table: "TB_GIV_InventoryMovement");

            migrationBuilder.RenameColumn(
                name: "FinishedGoodId",
                table: "TB_GIV_InventoryMovement",
                newName: "InventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_InventoryMovement_FinishedGoodId",
                table: "TB_GIV_InventoryMovement",
                newName: "IX_TB_GIV_InventoryMovement_InventoryId");

            migrationBuilder.AlterColumn<string>(
                name: "ContainerNo_GW",
                table: "TB_GIV_Container",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "JobNo_GW",
                table: "TB_GIV_Container",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnstuffedBy",
                table: "TB_GIV_Container",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UnstuffedDate",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "TB_GIV_Pallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
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
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFragile = table.Column<bool>(type: "boolean", nullable: false),
                    IsHazardous = table.Column<bool>(type: "boolean", nullable: false),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    RequiresExpirationDate = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresLotTracking = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresSerialNumber = table.Column<bool>(type: "boolean", nullable: false),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false)
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
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualReceivingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualUnstuffingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    PONumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransportTypeID = table.Column<int>(type: "integer", nullable: false),
                    UnstuffingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_InventoryMovement_TB_GIV_Inventory_InventoryId",
                table: "TB_GIV_InventoryMovement",
                column: "InventoryId",
                principalTable: "TB_GIV_Inventory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

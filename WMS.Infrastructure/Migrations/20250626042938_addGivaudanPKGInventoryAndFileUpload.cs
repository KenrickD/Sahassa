using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addGivaudanPKGInventoryAndFileUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FileUploadId",
                table: "TB_Inventory",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TB_FileUpload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_FileUpload", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_FileUploadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    S3Key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_FileUploadItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_FileUploadItems_TB_FileUpload_FileUploadId",
                        column: x => x.FileUploadId,
                        principalTable: "TB_FileUpload",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_PKG_Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUploadId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FromLocationAddressId = table.Column<int>(type: "integer", nullable: false),
                    ToLocationAddressId = table.Column<int>(type: "integer", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanReceiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualReceiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDG = table.Column<bool>(type: "boolean", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    PackSize = table.Column<int>(type: "integer", nullable: false),
                    NoItems = table.Column<int>(type: "integer", nullable: false),
                    NoOfPallet = table.Column<int>(type: "integer", nullable: false),
                    CurrentQty = table.Column<int>(type: "integer", nullable: false),
                    CurrentPackSize = table.Column<int>(type: "integer", nullable: false),
                    CurrentNoItems = table.Column<int>(type: "integer", nullable: false),
                    CurrentNoOfPallet = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_PKG_Inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_Inventory_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_Inventory_TB_FileUpload_FileUploadId",
                        column: x => x.FileUploadId,
                        principalTable: "TB_FileUpload",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_Inventory_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_Inventory_TB_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TB_Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_Inventory_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_PKG_InventoryMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIVPKGInventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromLocationAddressId = table.Column<int>(type: "integer", nullable: false),
                    ToLocationAddressId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    PackSize = table.Column<int>(type: "integer", nullable: false),
                    NoItems = table.Column<int>(type: "integer", nullable: false),
                    NoOfPallet = table.Column<int>(type: "integer", nullable: false),
                    IsReadyToBill = table.Column<bool>(type: "boolean", nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_PKG_InventoryMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_InventoryMovement_TB_GIV_PKG_Inventory_GIVPKGInv~",
                        column: x => x.GIVPKGInventoryId,
                        principalTable: "TB_GIV_PKG_Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_InventoryMovement_TB_Location_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_InventoryMovement_TB_Location_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_InventoryMovement_TB_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TB_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_PKG_InventoryMovement_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_Inventory_FileUploadId",
                table: "TB_Inventory",
                column: "FileUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUpload_CreatedAt",
                table: "TB_FileUpload",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUploadItems_FileType",
                table: "TB_FileUploadItems",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUploadItems_FileUploadId",
                table: "TB_FileUploadItems",
                column: "FileUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUploadItems_IsActive",
                table: "TB_FileUploadItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUploadItems_S3Key",
                table: "TB_FileUploadItems",
                column: "S3Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_Inventory_ClientId",
                table: "TB_GIV_PKG_Inventory",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_Inventory_FileUploadId",
                table: "TB_GIV_PKG_Inventory",
                column: "FileUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_Inventory_LocationId",
                table: "TB_GIV_PKG_Inventory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_Inventory_ProductId",
                table: "TB_GIV_PKG_Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_Inventory_WarehouseId",
                table: "TB_GIV_PKG_Inventory",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_InventoryMovement_FromLocationId",
                table: "TB_GIV_PKG_InventoryMovement",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_InventoryMovement_GIVPKGInventoryId",
                table: "TB_GIV_PKG_InventoryMovement",
                column: "GIVPKGInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_InventoryMovement_ProductId",
                table: "TB_GIV_PKG_InventoryMovement",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_InventoryMovement_ToLocationId",
                table: "TB_GIV_PKG_InventoryMovement",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_PKG_InventoryMovement_WarehouseId",
                table: "TB_GIV_PKG_InventoryMovement",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_Inventory_TB_FileUpload_FileUploadId",
                table: "TB_Inventory",
                column: "FileUploadId",
                principalTable: "TB_FileUpload",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_Inventory_TB_FileUpload_FileUploadId",
                table: "TB_Inventory");

            migrationBuilder.DropTable(
                name: "TB_FileUploadItems");

            migrationBuilder.DropTable(
                name: "TB_GIV_PKG_InventoryMovement");

            migrationBuilder.DropTable(
                name: "TB_GIV_PKG_Inventory");

            migrationBuilder.DropTable(
                name: "TB_FileUpload");

            migrationBuilder.DropIndex(
                name: "IX_TB_Inventory_FileUploadId",
                table: "TB_Inventory");

            migrationBuilder.DropColumn(
                name: "FileUploadId",
                table: "TB_Inventory");
        }
    }
}

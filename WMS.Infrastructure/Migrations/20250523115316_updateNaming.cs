using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_Receive_GIV_RM_Receive~",
                table: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_FG~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.RenameColumn(
                name: "GIV_FG_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "GIV_RM_ReceivePalletId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_FG_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceivePalletId");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceiveId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "GIV_RM_ReceivePalletId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceiveId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceivePalletId");

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceivePallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletCode = table.Column<int>(type: "integer", maxLength: 11, nullable: false),
                    HandledBy = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_TB_GIV_RM_ReceivePallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                        column: x => x.GIV_RM_ReceiveId,
                        principalTable: "TB_GIV_RM_Receive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_WarehouseId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_ReceivePallet_GIV_RM_R~",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceivePallet_GIV_~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_ReceivePallet_GIV_RM_R~",
                table: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceivePallet_GIV_~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "GIV_FG_ReceiveItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_FG_ReceiveItemId");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceivePalletId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "GIV_RM_ReceiveId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceivePalletId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceiveId");

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceiveItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    HandledBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    PalletCode = table.Column<int>(type: "integer", maxLength: 11, nullable: false),
                    StoredBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReceiveItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceiveItem_TB_GIV_FinishedGood_GIV_FinishedGoodId",
                        column: x => x.GIV_FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceiveItem_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceiveItem_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_GIV_FinishedGoodId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "GIV_FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_LocationId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_WarehouseId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_Receive_GIV_RM_Receive~",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "GIV_RM_ReceiveId",
                principalTable: "TB_GIV_RM_Receive",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_FG~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_FG_ReceiveItemId",
                principalTable: "TB_GIV_FG_ReceiveItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

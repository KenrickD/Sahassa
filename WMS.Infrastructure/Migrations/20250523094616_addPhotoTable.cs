using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addPhotoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_RM_ReceiveItem",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropColumn(
                name: "GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.RenameTable(
                name: "TB_GIV_RM_ReceiveItem",
                newName: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.RenameColumn(
                name: "StorageLocation_ID",
                table: "TB_GIV_FG_ReceiveItem",
                newName: "GIV_FinishedGoodId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_WarehouseId",
                table: "TB_GIV_FG_ReceiveItem",
                newName: "IX_TB_GIV_FG_ReceiveItem_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_LocationId",
                table: "TB_GIV_FG_ReceiveItem",
                newName: "IX_TB_GIV_FG_ReceiveItem_LocationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_FG_ReceiveItem",
                table: "TB_GIV_FG_ReceiveItem",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_GIV_FinishedGoodId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "GIV_FinishedGoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_GIV_FinishedGood_GIV_FinishedGoodId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "GIV_FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_Location_LocationId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "LocationId",
                principalTable: "TB_Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_FG_ReceiveItem",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceiveItemId",
                principalTable: "TB_GIV_FG_ReceiveItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_GIV_FinishedGood_GIV_FinishedGoodId",
                table: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_Location_LocationId",
                table: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_FG_ReceiveItem",
                table: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_GIV_FinishedGoodId",
                table: "TB_GIV_FG_ReceiveItem");

            migrationBuilder.RenameTable(
                name: "TB_GIV_FG_ReceiveItem",
                newName: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.RenameColumn(
                name: "GIV_FinishedGoodId",
                table: "TB_GIV_RM_ReceiveItem",
                newName: "StorageLocation_ID");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_WarehouseId",
                table: "TB_GIV_RM_ReceiveItem",
                newName: "IX_TB_GIV_RM_ReceiveItem_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItem_LocationId",
                table: "TB_GIV_RM_ReceiveItem",
                newName: "IX_TB_GIV_RM_ReceiveItem_LocationId");

            migrationBuilder.AddColumn<Guid>(
                name: "GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_RM_ReceiveItem",
                table: "TB_GIV_RM_ReceiveItem",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceiveItem_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "GIV_RM_ReceiveId",
                principalTable: "TB_GIV_RM_Receive",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "LocationId",
                principalTable: "TB_Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceiveItem",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceiveItemId",
                principalTable: "TB_GIV_RM_ReceiveItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

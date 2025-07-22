using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatetblname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceivePallet_GIV_~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_RM_ReceiveItemBreakdown",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.RenameTable(
                name: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletItem",
                newName: "IX_TB_GIV_RM_ReceivePalletItem_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceivePalletItem",
                newName: "IX_TB_GIV_RM_ReceivePalletItem_GIV_RM_ReceivePalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_RM_ReceivePalletItem",
                table: "TB_GIV_RM_ReceivePalletItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletItem_TB_GIV_RM_ReceivePallet_GIV_RM_~",
                table: "TB_GIV_RM_ReceivePalletItem",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletItem",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletItem_TB_GIV_RM_ReceivePallet_GIV_RM_~",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletItem_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_RM_ReceivePalletItem",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.RenameTable(
                name: "TB_GIV_RM_ReceivePalletItem",
                newName: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceivePalletItem_WarehouseId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceivePalletItem_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceivePalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_RM_ReceiveItemBreakdown",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_RM_ReceivePallet_GIV_~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

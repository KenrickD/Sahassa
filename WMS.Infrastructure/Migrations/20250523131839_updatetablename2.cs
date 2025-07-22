using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatetablename2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_ReceivePallet_GIV_RM_R~",
                table: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_Warehouse_WarehouseId",
                table: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_FG_ReceiveItemPhoto",
                table: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.RenameTable(
                name: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                newName: "IX_TB_GIV_RM_ReceivePalletPhoto_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                newName: "IX_TB_GIV_RM_ReceivePalletPhoto_GIV_RM_ReceivePalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_RM_ReceivePalletPhoto",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_GIV_RM_ReceivePallet_GIV_RM~",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_GIV_RM_ReceivePallet_GIV_RM~",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_Warehouse_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TB_GIV_RM_ReceivePalletPhoto",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.RenameTable(
                name: "TB_GIV_RM_ReceivePalletPhoto",
                newName: "TB_GIV_FG_ReceiveItemPhoto");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceivePalletPhoto_WarehouseId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "IX_TB_GIV_FG_ReceiveItemPhoto_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceivePalletPhoto_GIV_RM_ReceivePalletId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                newName: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceivePalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TB_GIV_FG_ReceiveItemPhoto",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_ReceivePallet_GIV_RM_R~",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_Warehouse_WarehouseId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

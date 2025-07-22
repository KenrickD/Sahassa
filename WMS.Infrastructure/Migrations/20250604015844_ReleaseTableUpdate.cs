using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePallet_GIV_RM_Rec~",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails",
                newName: "GIV_RM_ReceivePalletItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails",
                newName: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletItemId",
                principalTable: "TB_GIV_RM_ReceivePalletItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceivePalletItemId",
                table: "TB_GIV_RM_ReleaseDetails",
                newName: "GIV_RM_ReceivePalletId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletItemId",
                table: "TB_GIV_RM_ReleaseDetails",
                newName: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePallet_GIV_RM_Rec~",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

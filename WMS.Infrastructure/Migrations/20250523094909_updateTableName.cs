using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.RenameColumn(
                name: "GIV_RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "GIV_FG_ReceiveItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_FG_ReceiveItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_FG~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_FG_ReceiveItemId",
                principalTable: "TB_GIV_FG_ReceiveItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_FG~",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.RenameColumn(
                name: "GIV_FG_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "GIV_RM_ReceiveItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_FG_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "IX_TB_GIV_RM_ReceiveItemBreakdown_GIV_RM_ReceiveItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceiveItemBreakdown_TB_GIV_FG_ReceiveItem_GIV_RM~",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                column: "GIV_RM_ReceiveItemId",
                principalTable: "TB_GIV_FG_ReceiveItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

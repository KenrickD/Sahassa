using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addcolumnreleasedetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GIV_FG_ReceivePalletItemId",
                table: "TB_GIV_FG_ReleaseDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEntirePallet",
                table: "TB_GIV_FG_ReleaseDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReceivePalletItemId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceivePalletItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_ReceivePalletItem_GIV_FG~",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceivePalletItemId",
                principalTable: "TB_GIV_FG_ReceivePalletItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceiveId",
                principalTable: "TB_GIV_FG_Receive",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_ReceivePalletItem_GIV_FG~",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReceivePalletItemId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropColumn(
                name: "GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropColumn(
                name: "GIV_FG_ReceivePalletItemId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropColumn(
                name: "IsEntirePallet",
                table: "TB_GIV_FG_ReleaseDetails");
        }
    }
}

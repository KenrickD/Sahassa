using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterFGReceivePalletItemLinkFG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinishedGoodId",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePalletItem_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePalletItem",
                column: "FinishedGoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePalletItem_TB_GIV_FinishedGood_FinishedGoo~",
                table: "TB_GIV_FG_ReceivePalletItem",
                column: "FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePalletItem_TB_GIV_FinishedGood_FinishedGoo~",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReceivePalletItem_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "FinishedGoodId",
                table: "TB_GIV_FG_ReceivePalletItem");
        }
    }
}

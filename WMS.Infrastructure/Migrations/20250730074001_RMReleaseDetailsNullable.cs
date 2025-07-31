using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RMReleaseDetailsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.AlterColumn<Guid>(
                name: "GIV_RM_ReceivePalletItemId",
                table: "TB_GIV_RM_ReleaseDetails",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletItemId",
                principalTable: "TB_GIV_RM_ReceivePalletItem",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.AlterColumn<Guid>(
                name: "GIV_RM_ReceivePalletItemId",
                table: "TB_GIV_RM_ReleaseDetails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePalletItem_GIV_RM~",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletItemId",
                principalTable: "TB_GIV_RM_ReceivePalletItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

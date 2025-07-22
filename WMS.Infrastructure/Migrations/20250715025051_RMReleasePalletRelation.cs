using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RMReleasePalletRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEntirePallet",
                table: "TB_GIV_RM_ReleaseDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePallet_GIV_RM_Rec~",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletId",
                principalTable: "TB_GIV_RM_ReceivePallet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePallet_GIV_RM_Rec~",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.DropColumn(
                name: "GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.DropColumn(
                name: "IsEntirePallet",
                table: "TB_GIV_RM_ReleaseDetails");
        }
    }
}

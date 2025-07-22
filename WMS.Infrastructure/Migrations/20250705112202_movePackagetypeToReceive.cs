using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class movePackagetypeToReceive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_FG_Receive",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_PackageTypeId",
                table: "TB_GIV_RM_Receive",
                column: "PackageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_Receive_PackageTypeId",
                table: "TB_GIV_FG_Receive",
                column: "PackageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_Receive_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_Receive",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_Receive",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_Receive_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_Receive");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_Receive_PackageTypeId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_Receive_PackageTypeId",
                table: "TB_GIV_FG_Receive");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_FG_Receive");

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "PackageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "PackageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");
        }
    }
}

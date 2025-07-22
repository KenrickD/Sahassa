using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateRMTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DG",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProdDate",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackSize",
                table: "TB_GIV_RM_ReceivePallet",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "TB_GIV_RM_Receive",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DG",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "ProdDate",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "TB_GIV_RM_Receive");
        }
    }
}

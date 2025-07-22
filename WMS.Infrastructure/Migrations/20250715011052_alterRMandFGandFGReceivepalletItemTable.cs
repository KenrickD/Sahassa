using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterRMandFGandFGReceivepalletItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackSize",
                table: "TB_GIV_RawMaterial",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackSize",
                table: "TB_GIV_FinishedGood",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProdDate",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "TB_GIV_FinishedGood");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProdDate",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}

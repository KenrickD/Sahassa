using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFGTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackSize",
                table: "TB_GIV_FG_ReceivePallet",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "TB_GIV_FG_ReceivePallet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "TB_GIV_FG_ReceivePallet",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "TB_GIV_FG_Receive",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "TB_GIV_FG_Receive");
        }
    }
}

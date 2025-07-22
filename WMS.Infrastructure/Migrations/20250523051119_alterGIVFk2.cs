using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterGIVFk2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown");

            migrationBuilder.DropColumn(
                name: "RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem");

            migrationBuilder.DropColumn(
                name: "FG_ID",
                table: "TB_GIV_FGInventory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FG_ID",
                table: "TB_GIV_FGInventory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterGIVFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropColumn(
                name: "StorageLocation_ID",
                table: "TB_GIV_FGInventory");

            migrationBuilder.RenameColumn(
                name: "RM_ReceiveItem_ID",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "RM_ReceiveItemId");

            migrationBuilder.RenameColumn(
                name: "RM_Receive_ID",
                table: "TB_GIV_RM_ReceiveItem",
                newName: "RM_ReceiveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RM_ReceiveItemId",
                table: "TB_GIV_RM_ReceiveItemBreakdown",
                newName: "RM_ReceiveItem_ID");

            migrationBuilder.RenameColumn(
                name: "RM_ReceiveId",
                table: "TB_GIV_RM_ReceiveItem",
                newName: "RM_Receive_ID");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StorageLocation_ID",
                table: "TB_GIV_FGInventory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}

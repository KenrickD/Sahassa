using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateReceivePallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.AlterColumn<string>(
                name: "StoredBy",
                table: "TB_GIV_RM_ReceivePallet",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "LocationId",
                principalTable: "TB_Location",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.AlterColumn<string>(
                name: "StoredBy",
                table: "TB_GIV_RM_ReceivePallet",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "LocationId",
                principalTable: "TB_Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

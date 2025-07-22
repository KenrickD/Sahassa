using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.AlterColumn<Guid>(
                name: "GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceiveId",
                principalTable: "TB_GIV_FG_Receive",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.AlterColumn<Guid>(
                name: "GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Receive_GIV_FG_ReceiveId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceiveId",
                principalTable: "TB_GIV_FG_Receive",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

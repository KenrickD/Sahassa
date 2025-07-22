using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterFGReceivePallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.AlterColumn<Guid>(
                name: "FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.AlterColumn<Guid>(
                name: "FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

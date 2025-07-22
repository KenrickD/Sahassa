using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alter_rm_receive3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.AlterColumn<Guid>(
                name: "Container_ID",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerId",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId",
                principalTable: "TB_GIV_Container",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.AlterColumn<Guid>(
                name: "Container_ID",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerId",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId",
                principalTable: "TB_GIV_Container",
                principalColumn: "Id");
        }
    }
}

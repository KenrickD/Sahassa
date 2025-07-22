using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterContainerWarehouseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                table: "TB_GIV_Container");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "TB_GIV_Container",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                table: "TB_GIV_Container",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                table: "TB_GIV_Container");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "TB_GIV_Container",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                table: "TB_GIV_Container",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

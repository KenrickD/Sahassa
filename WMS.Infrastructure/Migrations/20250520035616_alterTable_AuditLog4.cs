using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterTable_AuditLog4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_AuditLog_TB_Warehouse_WarehouseId",
                table: "TB_AuditLog");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_AuditLog_TB_Warehouse_WarehouseId",
                table: "TB_AuditLog",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_AuditLog_TB_Warehouse_WarehouseId",
                table: "TB_AuditLog");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_AuditLog_TB_Warehouse_WarehouseId",
                table: "TB_AuditLog",
                column: "WarehouseId",
                principalTable: "TB_Warehouse",
                principalColumn: "Id");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class unimplementmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_TB_InventoryMovement_TB_Inventory_InventoryId",
            //    table: "TB_InventoryMovement");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "InventoryId",
            //    table: "TB_InventoryMovement",
            //    type: "uuid",
            //    nullable: true,
            //    oldClrType: typeof(Guid),
            //    oldType: "uuid");

            //migrationBuilder.AddColumn<Guid>(
            //    name: "EntityId",
            //    table: "TB_InventoryMovement",
            //    type: "uuid",
            //    nullable: false,
            //    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            //migrationBuilder.AddColumn<string>(
            //    name: "EntityName",
            //    table: "TB_InventoryMovement",
            //    type: "text",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_TB_InventoryMovement_TB_Inventory_InventoryId",
            //    table: "TB_InventoryMovement",
            //    column: "InventoryId",
            //    principalTable: "TB_Inventory",
            //    principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_TB_InventoryMovement_TB_Inventory_InventoryId",
            //    table: "TB_InventoryMovement");

            //migrationBuilder.DropColumn(
            //    name: "EntityId",
            //    table: "TB_InventoryMovement");

            //migrationBuilder.DropColumn(
            //    name: "EntityName",
            //    table: "TB_InventoryMovement");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "InventoryId",
            //    table: "TB_InventoryMovement",
            //    type: "uuid",
            //    nullable: false,
            //    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            //    oldClrType: typeof(Guid),
            //    oldType: "uuid",
            //    oldNullable: true);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_TB_InventoryMovement_TB_Inventory_InventoryId",
            //    table: "TB_InventoryMovement",
            //    column: "InventoryId",
            //    principalTable: "TB_Inventory",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }
    }
}

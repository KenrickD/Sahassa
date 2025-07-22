using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newTablesForGeneralCodeLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductCategoryId",
                table: "TB_Product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductTypeId",
                table: "TB_Product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureCodeId",
                table: "TB_Product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TB_GeneralCodeType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GeneralCodeType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GeneralCodeType_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GeneralCode",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneralCodeTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GeneralCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GeneralCode_TB_GeneralCodeType_GeneralCodeTypeId",
                        column: x => x.GeneralCodeTypeId,
                        principalTable: "TB_GeneralCodeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GeneralCode_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_Product_ProductCategoryId",
                table: "TB_Product",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Product_ProductTypeId",
                table: "TB_Product",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Product_UnitOfMeasureCodeId",
                table: "TB_Product",
                column: "UnitOfMeasureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GeneralCode_GeneralCodeTypeId",
                table: "TB_GeneralCode",
                column: "GeneralCodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GeneralCode_WarehouseId",
                table: "TB_GeneralCode",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GeneralCodeType_WarehouseId",
                table: "TB_GeneralCodeType",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_ProductCategoryId",
                table: "TB_Product",
                column: "ProductCategoryId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_ProductTypeId",
                table: "TB_Product",
                column: "ProductTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_UnitOfMeasureCodeId",
                table: "TB_Product",
                column: "UnitOfMeasureCodeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_ProductCategoryId",
                table: "TB_Product");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_ProductTypeId",
                table: "TB_Product");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_Product_TB_GeneralCode_UnitOfMeasureCodeId",
                table: "TB_Product");

            migrationBuilder.DropTable(
                name: "TB_GeneralCode");

            migrationBuilder.DropTable(
                name: "TB_GeneralCodeType");

            migrationBuilder.DropIndex(
                name: "IX_TB_Product_ProductCategoryId",
                table: "TB_Product");

            migrationBuilder.DropIndex(
                name: "IX_TB_Product_ProductTypeId",
                table: "TB_Product");

            migrationBuilder.DropIndex(
                name: "IX_TB_Product_UnitOfMeasureCodeId",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureCodeId",
                table: "TB_Product");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterProductTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "MaxStockLevel",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "MinStockLevel",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "ReorderQuantity",
                table: "TB_Product");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "TB_Product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TB_Product",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxStockLevel",
                table: "TB_Product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinStockLevel",
                table: "TB_Product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderPoint",
                table: "TB_Product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderQuantity",
                table: "TB_Product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "TB_Product",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addgrptoRM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Group3",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group6",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group8",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group9",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NDG",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Scentaurus",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group3",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "Group6",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "Group8",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "Group9",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "NDG",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "Scentaurus",
                table: "TB_GIV_RawMaterial");
        }
    }
}

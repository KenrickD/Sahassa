using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterTableContainterAddLooseColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoose",
                table: "TB_GIV_Container",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LooseNoItem",
                table: "TB_GIV_Container",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LooseNoPallet",
                table: "TB_GIV_Container",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoose",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "LooseNoItem",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "LooseNoPallet",
                table: "TB_GIV_Container");
        }
    }
}

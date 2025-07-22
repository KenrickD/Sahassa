using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addnewgrouptoFGandRM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Group4_1",
                table: "TB_GIV_RawMaterial",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group4_1",
                table: "TB_GIV_FinishedGood",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group4_1",
                table: "TB_GIV_RawMaterial");

            migrationBuilder.DropColumn(
                name: "Group4_1",
                table: "TB_GIV_FinishedGood");
        }
    }
}

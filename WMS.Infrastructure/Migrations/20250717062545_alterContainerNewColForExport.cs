using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterContainerNewColForExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobReference",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessType",
                table: "TB_GIV_Container",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SealNo",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Size",
                table: "TB_GIV_Container",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobReference",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "ProcessType",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "SealNo",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "TB_GIV_Container");
        }
    }
}

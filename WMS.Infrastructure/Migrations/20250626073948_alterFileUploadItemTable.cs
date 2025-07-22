using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterFileUploadItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TB_FileUploadItems_IsActive",
                table: "TB_FileUploadItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TB_FileUploadItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TB_FileUploadItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_FileUploadItems_IsActive",
                table: "TB_FileUploadItems",
                column: "IsActive");
        }
    }
}

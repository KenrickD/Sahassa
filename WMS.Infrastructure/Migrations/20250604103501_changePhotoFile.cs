using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changePhotoFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "TB_GIV_FG_ReceivePalletPhoto");

            migrationBuilder.RenameColumn(
                name: "PhotoName",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                newName: "PhotoFile");

            migrationBuilder.RenameColumn(
                name: "PhotoName",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                newName: "PhotoFile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhotoFile",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                newName: "PhotoName");

            migrationBuilder.RenameColumn(
                name: "PhotoFile",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                newName: "PhotoName");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

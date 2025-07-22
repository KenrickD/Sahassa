using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addFileColumntoPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileType",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "TB_GIV_RM_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TB_GIV_FG_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TB_GIV_FG_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "TB_GIV_FG_ReceivePalletPhoto");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "TB_GIV_FG_ReceivePalletPhoto");
        }
    }
}

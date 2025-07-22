using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterTable_AuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "TB_AuditLog");

            migrationBuilder.RenameColumn(
                name: "OldValues",
                table: "TB_AuditLog",
                newName: "ChangesJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChangesJson",
                table: "TB_AuditLog",
                newName: "OldValues");

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "TB_AuditLog",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

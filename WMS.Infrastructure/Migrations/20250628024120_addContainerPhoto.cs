using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addContainerPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhotoFile",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoFile",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "UnstuffedBy",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnstuffedDate",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TB_GIV_ContainerPhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoFile = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_ContainerPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_ContainerPhoto_TB_GIV_Container_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "TB_GIV_Container",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_ContainerPhoto_ContainerId",
                table: "TB_GIV_ContainerPhoto",
                column: "ContainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_ContainerPhoto");

            migrationBuilder.DropColumn(
                name: "UnstuffedBy",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "UnstuffedDate",
                table: "TB_GIV_Container");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoFile",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoFile",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

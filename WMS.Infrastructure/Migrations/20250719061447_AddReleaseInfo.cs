using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualReleaseDate",
                table: "TB_GIV_RM_Release",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualReleasedBy",
                table: "TB_GIV_RM_Release",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualReleaseDate",
                table: "TB_GIV_FG_Release",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualReleasedBy",
                table: "TB_GIV_FG_Release",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualReleaseDate",
                table: "TB_GIV_RM_Release");

            migrationBuilder.DropColumn(
                name: "ActualReleasedBy",
                table: "TB_GIV_RM_Release");

            migrationBuilder.DropColumn(
                name: "ActualReleaseDate",
                table: "TB_GIV_FG_Release");

            migrationBuilder.DropColumn(
                name: "ActualReleasedBy",
                table: "TB_GIV_FG_Release");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterContainerNewColumnsForExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StuffedDate",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StuffStartTime",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StuffEndTime",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StuffedBy",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StuffEndTime",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "StuffStartTime",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "StuffedBy",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "StuffedDate",
                table: "TB_GIV_Container");
        }
    }
}

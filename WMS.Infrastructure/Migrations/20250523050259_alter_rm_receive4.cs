using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alter_rm_receive4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Container_ID",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.RenameColumn(
                name: "Material_ID",
                table: "TB_GIV_RM_Receive",
                newName: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "TB_GIV_RM_Receive",
                newName: "Material_ID");

            migrationBuilder.AddColumn<Guid>(
                name: "Container_ID",
                table: "TB_GIV_RM_Receive",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}

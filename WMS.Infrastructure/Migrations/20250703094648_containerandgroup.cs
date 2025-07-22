using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class containerandgroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerNo",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "ETA",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "JobNo_GW",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "POL",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "SealNo",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "VesselName",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "VoyageNo",
                table: "TB_GIV_Container");

            migrationBuilder.AddColumn<string>(
                name: "Grp",
                table: "TB_GIV_RM_ReceivePallet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grp",
                table: "TB_GIV_FG_ReceivePallet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "PackageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "PackageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "PackageTypeId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_ReceivePallet_TB_GeneralCode_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Grp",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Grp",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.AddColumn<string>(
                name: "ContainerNo",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ETA",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobNo_GW",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "POL",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SealNo",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselName",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoyageNo",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);
        }
    }
}

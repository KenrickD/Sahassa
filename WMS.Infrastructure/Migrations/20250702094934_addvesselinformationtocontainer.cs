using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addvesselinformationtocontainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PO",
                table: "TB_GIV_RM_Receive",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PO",
                table: "TB_GIV_FG_Receive",
                type: "text",
                nullable: true);

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
                name: "HBL",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobNo_GW",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PO",
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

            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                table: "TB_GIV_Container",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnstuffEndTime",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnstuffStartTime",
                table: "TB_GIV_Container",
                type: "timestamp with time zone",
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

            migrationBuilder.AddColumn<string>(
                name: "YourRef",
                table: "TB_GIV_Container",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Container_StatusId",
                table: "TB_GIV_Container",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_Container_TB_GeneralCode_StatusId",
                table: "TB_GIV_Container",
                column: "StatusId",
                principalTable: "TB_GeneralCode",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_Container_TB_GeneralCode_StatusId",
                table: "TB_GIV_Container");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_Container_StatusId",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "PO",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropColumn(
                name: "PO",
                table: "TB_GIV_FG_Receive");

            migrationBuilder.DropColumn(
                name: "ContainerNo",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "ETA",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "HBL",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "JobNo_GW",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "PO",
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
                name: "StatusId",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "UnstuffEndTime",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "UnstuffStartTime",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "VesselName",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "VoyageNo",
                table: "TB_GIV_Container");

            migrationBuilder.DropColumn(
                name: "YourRef",
                table: "TB_GIV_Container");
        }
    }
}

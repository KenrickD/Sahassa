using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dropcontainer2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_ContainerPhoto_TB_GIV_Container_ContainerId",
                table: "TB_GIV_ContainerPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropTable(
                name: "TB_GIV_Container");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_RM_Receive_ContainerId",
                table: "TB_GIV_RM_Receive");

            migrationBuilder.DropIndex(
                name: "IX_TB_GIV_ContainerPhoto_ContainerId",
                table: "TB_GIV_ContainerPhoto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_GIV_Container",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContainerNo = table.Column<string>(type: "text", nullable: true),
                    ContainerNo_GW = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContainerURL = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ETA = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HBL = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    JobNo_GW = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    PO = table.Column<string>(type: "text", nullable: true),
                    POL = table.Column<string>(type: "text", nullable: true),
                    PlannedDelivery_GW = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    SealNo = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<string>(type: "text", nullable: true),
                    UnstuffEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnstuffStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnstuffedBy = table.Column<string>(type: "text", nullable: true),
                    UnstuffedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VesselName = table.Column<string>(type: "text", nullable: true),
                    VoyageNo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Container", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Container_TB_GeneralCode_StatusId",
                        column: x => x.StatusId,
                        principalTable: "TB_GeneralCode",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_ContainerPhoto_ContainerId",
                table: "TB_GIV_ContainerPhoto",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Container_StatusId",
                table: "TB_GIV_Container",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Container_WarehouseId",
                table: "TB_GIV_Container",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_ContainerPhoto_TB_GIV_Container_ContainerId",
                table: "TB_GIV_ContainerPhoto",
                column: "ContainerId",
                principalTable: "TB_GIV_Container",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId",
                principalTable: "TB_GIV_Container",
                principalColumn: "Id");
        }
    }
}

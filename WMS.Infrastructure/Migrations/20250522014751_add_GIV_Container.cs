using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_GIV_Container : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_GIV_Container",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobNo_GW = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContainerNo_GW = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UnstuffedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnstuffedBy = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Container", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Container_WarehouseId",
                table: "TB_GIV_Container",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_Container");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addPhotoTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceiveItemPhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReceiveItemPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_GIV_RM_Receive_GIV_RM_Receive~",
                        column: x => x.GIV_RM_ReceiveId,
                        principalTable: "TB_GIV_RM_Receive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceiveItemPhoto_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_GIV_RM_ReceiveId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceiveItemPhoto_WarehouseId",
                table: "TB_GIV_FG_ReceiveItemPhoto",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceiveItemPhoto");
        }
    }
}

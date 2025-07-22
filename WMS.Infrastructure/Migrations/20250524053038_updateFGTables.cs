using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateFGTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_FGInventory");

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceivePallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PalletCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    HandledBy = table.Column<string>(type: "text", nullable: true),
                    StoredBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReceivePallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_LocationId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_WarehouseId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.CreateTable(
                name: "TB_GIV_FGInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    HandledBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    PalletCode = table.Column<int>(type: "integer", maxLength: 11, nullable: true),
                    StoredBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FGInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FGInventory_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_FinishedGoodId",
                table: "TB_GIV_FGInventory",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_LocationId",
                table: "TB_GIV_FGInventory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FGInventory_WarehouseId",
                table: "TB_GIV_FGInventory",
                column: "WarehouseId");
        }
    }
}

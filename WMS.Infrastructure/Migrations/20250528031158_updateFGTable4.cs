using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateFGTable4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.RenameColumn(
                name: "FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                newName: "ReceiveId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                newName: "IX_TB_GIV_FG_ReceivePallet_ReceiveId");

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceivePalletItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_FG_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BatchNo = table.Column<string>(type: "text", nullable: true),
                    ProdDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DG = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_TB_GIV_FG_ReceivePalletItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePalletItem_TB_GIV_FG_ReceivePallet_GIV_FG_~",
                        column: x => x.GIV_FG_ReceivePalletId,
                        principalTable: "TB_GIV_FG_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePalletItem_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePalletItem_GIV_FG_ReceivePalletId",
                table: "TB_GIV_FG_ReceivePalletItem",
                column: "GIV_FG_ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePalletItem_WarehouseId",
                table: "TB_GIV_FG_ReceivePalletItem",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FG_Receive_ReceiveId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "ReceiveId",
                principalTable: "TB_GIV_FG_Receive",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FG_Receive_ReceiveId",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.RenameColumn(
                name: "ReceiveId",
                table: "TB_GIV_FG_ReceivePallet",
                newName: "FinishedGoodId");

            migrationBuilder.RenameIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_ReceiveId",
                table: "TB_GIV_FG_ReceivePallet",
                newName: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "FinishedGoodId",
                principalTable: "TB_GIV_FinishedGood",
                principalColumn: "Id");
        }
    }
}

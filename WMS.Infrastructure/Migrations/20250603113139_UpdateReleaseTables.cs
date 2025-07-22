using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReleaseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
            //    table: "TB_GIV_FG_ReceivePallet");

            //migrationBuilder.RenameColumn(
            //    name: "FinishedGoodId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    newName: "ReceiveId");

            //migrationBuilder.RenameIndex(
            //    name: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    newName: "IX_TB_GIV_FG_ReceivePallet_ReceiveId");

            //migrationBuilder.AddColumn<bool>(
            //    name: "DG",
            //    table: "TB_GIV_RM_ReceivePalletItem",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReleased",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ProdDate",
            //    table: "TB_GIV_RM_ReceivePalletItem",
            //    type: "timestamp with time zone",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            //migrationBuilder.AddColumn<string>(
            //    name: "Remarks",
            //    table: "TB_GIV_RM_ReceivePalletItem",
            //    type: "text",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsReleased",
            //    table: "TB_GIV_RM_ReceivePallet",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<int>(
            //    name: "PackSize",
            //    table: "TB_GIV_RM_ReceivePallet",
            //    type: "integer",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "Remarks",
            //    table: "TB_GIV_RM_Receive",
            //    type: "text",
            //    nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReleased",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            //migrationBuilder.AddColumn<int>(
            //    name: "PackSize",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    type: "integer",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "ReceivedBy",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    type: "text",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ReceivedDate",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    type: "timestamp with time zone",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            //migrationBuilder.AddColumn<string>(
            //    name: "Remarks",
            //    table: "TB_GIV_FG_Receive",
            //    type: "text",
            //    nullable: true);

            //migrationBuilder.CreateTable(
            //    name: "TB_GIV_FG_ReceivePalletItem",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "uuid", nullable: false),
            //        GIV_FG_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
            //        ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            //        BatchNo = table.Column<string>(type: "text", nullable: true),
            //        ProdDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            //        DG = table.Column<bool>(type: "boolean", nullable: false),
            //        IsReleased = table.Column<bool>(type: "boolean", nullable: false),
            //        Remarks = table.Column<string>(type: "text", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            //        CreatedBy = table.Column<string>(type: "text", nullable: false),
            //        ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            //        ModifiedBy = table.Column<string>(type: "text", nullable: true),
            //        IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            //        WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_TB_GIV_FG_ReceivePalletItem", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_TB_GIV_FG_ReceivePalletItem_TB_GIV_FG_ReceivePallet_GIV_FG_~",
            //            column: x => x.GIV_FG_ReceivePalletId,
            //            principalTable: "TB_GIV_FG_ReceivePallet",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_TB_GIV_FG_ReceivePalletItem_TB_Warehouse_WarehouseId",
            //            column: x => x.WarehouseId,
            //            principalTable: "TB_Warehouse",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_Release",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedBy = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_TB_GIV_FG_Release", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_Release_TB_GIV_FinishedGood_GIV_FinishedGoodId",
                        column: x => x.GIV_FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_Release_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_Release",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RawMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedBy = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_TB_GIV_RM_Release", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Release_TB_GIV_RawMaterial_GIV_RawMaterialId",
                        column: x => x.GIV_RawMaterialId,
                        principalTable: "TB_GIV_RawMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Release_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReleaseDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_FG_ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_FG_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReleaseDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_ReceivePallet_GIV_FG_Rec~",
                        column: x => x.GIV_FG_ReceivePalletId,
                        principalTable: "TB_GIV_FG_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReleaseDetails_TB_GIV_FG_Release_GIV_FG_ReleaseId",
                        column: x => x.GIV_FG_ReleaseId,
                        principalTable: "TB_GIV_FG_Release",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReleaseDetails_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReleaseDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReleaseDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_ReceivePallet_GIV_RM_Rec~",
                        column: x => x.GIV_RM_ReceivePalletId,
                        principalTable: "TB_GIV_RM_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                        column: x => x.GIV_RM_ReceiveId,
                        principalTable: "TB_GIV_RM_Receive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReleaseDetails_TB_GIV_RM_Release_GIV_RM_ReleaseId",
                        column: x => x.GIV_RM_ReleaseId,
                        principalTable: "TB_GIV_RM_Release",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReleaseDetails_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            //migrationBuilder.CreateIndex(
            //    name: "IX_TB_GIV_FG_ReceivePalletItem_GIV_FG_ReceivePalletId",
            //    table: "TB_GIV_FG_ReceivePalletItem",
            //    column: "GIV_FG_ReceivePalletId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_TB_GIV_FG_ReceivePalletItem_WarehouseId",
            //    table: "TB_GIV_FG_ReceivePalletItem",
            //    column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_Release_GIV_FinishedGoodId",
                table: "TB_GIV_FG_Release",
                column: "GIV_FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_Release_WarehouseId",
                table: "TB_GIV_FG_Release",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReceivePalletId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_GIV_FG_ReleaseId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "GIV_FG_ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReleaseDetails_WarehouseId",
                table: "TB_GIV_FG_ReleaseDetails",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Release_GIV_RawMaterialId",
                table: "TB_GIV_RM_Release",
                column: "GIV_RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Release_WarehouseId",
                table: "TB_GIV_RM_Release",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_GIV_RM_ReleaseId",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "GIV_RM_ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReleaseDetails_WarehouseId",
                table: "TB_GIV_RM_ReleaseDetails",
                column: "WarehouseId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FG_Receive_ReceiveId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    column: "ReceiveId",
            //    principalTable: "TB_GIV_FG_Receive",
            //    principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FG_Receive_ReceiveId",
            //    table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReleaseDetails");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReleaseDetails");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_Release");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_Release");

            //migrationBuilder.DropColumn(
            //    name: "DG",
            //    table: "TB_GIV_RM_ReceivePalletItem");

            //migrationBuilder.DropColumn(
            //    name: "IsReleased",
            //    table: "TB_GIV_RM_ReceivePalletItem");

            //migrationBuilder.DropColumn(
            //    name: "ProdDate",
            //    table: "TB_GIV_RM_ReceivePalletItem");

            //migrationBuilder.DropColumn(
            //    name: "Remarks",
            //    table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "IsReleased",
                table: "TB_GIV_RM_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "PackSize",
            //    table: "TB_GIV_RM_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "Remarks",
            //    table: "TB_GIV_RM_Receive");

            migrationBuilder.DropColumn(
                name: "IsReleased",
                table: "TB_GIV_FG_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "PackSize",
            //    table: "TB_GIV_FG_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "ReceivedBy",
            //    table: "TB_GIV_FG_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "ReceivedDate",
            //    table: "TB_GIV_FG_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "Remarks",
            //    table: "TB_GIV_FG_Receive");

            //migrationBuilder.RenameColumn(
            //    name: "ReceiveId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    newName: "FinishedGoodId");

            //migrationBuilder.RenameIndex(
            //    name: "IX_TB_GIV_FG_ReceivePallet_ReceiveId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    newName: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    column: "FinishedGoodId",
            //    principalTable: "TB_GIV_FinishedGood",
            //    principalColumn: "Id");
        }
    }
}

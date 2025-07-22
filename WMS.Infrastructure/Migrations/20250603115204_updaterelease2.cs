using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updaterelease2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "IsReleased",
            //    table: "TB_GIV_RM_ReceivePallet");

            //migrationBuilder.DropColumn(
            //    name: "IsReleased",
            //    table: "TB_GIV_FG_ReceivePalletItem");

            //migrationBuilder.DropColumn(
            //    name: "IsReleased",
            //    table: "TB_GIV_FG_ReceivePallet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<bool>(
            //    name: "IsReleased",
            //    table: "TB_GIV_RM_ReceivePallet",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsReleased",
            //    table: "TB_GIV_FG_ReceivePalletItem",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsReleased",
            //    table: "TB_GIV_FG_ReceivePallet",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: false);
        }
    }
}

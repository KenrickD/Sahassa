using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class alterLocationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Row",
                table: "TB_Location",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessType",
                table: "TB_Location",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Aisle",
                table: "TB_Location",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bin",
                table: "TB_Location",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullLocationCode",
                table: "TB_Location",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickingPriority",
                table: "TB_Location",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Side",
                table: "TB_Location",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemperatureZone",
                table: "TB_Location",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "Type",
            //    table: "TB_Location",
            //    type: "integer",
            //    nullable: false,
            //    defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "XCoordinate",
                table: "TB_Location",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YCoordinate",
                table: "TB_Location",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ZCoordinate",
                table: "TB_Location",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_FullLocationCode",
                table: "TB_Location",
                column: "FullLocationCode");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_Row",
                table: "TB_Location",
                column: "Row");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_Bay",
                table: "TB_Location",
                column: "Bay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "Aisle",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "Bin",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "FullLocationCode",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "PickingPriority",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "Side",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "TemperatureZone",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "XCoordinate",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "YCoordinate",
                table: "TB_Location");

            migrationBuilder.DropColumn(
                name: "ZCoordinate",
                table: "TB_Location");

            migrationBuilder.AlterColumn<int>(
                name: "Row",
                table: "TB_Location",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}

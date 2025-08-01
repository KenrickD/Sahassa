﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGroupLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group3",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group6",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group8",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group9",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "NDG",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Scentaurus",
                table: "TB_GIV_RM_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group3",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group6",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group8",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Group9",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "NDG",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropColumn(
                name: "Scentaurus",
                table: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.RenameColumn(
                name: "DG",
                table: "TB_GIV_RM_ReceivePalletItem",
                newName: "Scentaurus");

            migrationBuilder.RenameColumn(
                name: "DG",
                table: "TB_GIV_FG_ReceivePalletItem",
                newName: "Scentaurus");

            migrationBuilder.AddColumn<bool>(
                name: "Group3",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group6",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group8",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group9",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NDG",
                table: "TB_GIV_RM_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group3",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group6",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group8",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group9",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NDG",
                table: "TB_GIV_FG_ReceivePalletItem",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group3",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group6",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group8",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group9",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "NDG",
                table: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group3",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group6",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group8",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "Group9",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.DropColumn(
                name: "NDG",
                table: "TB_GIV_FG_ReceivePalletItem");

            migrationBuilder.RenameColumn(
                name: "Scentaurus",
                table: "TB_GIV_RM_ReceivePalletItem",
                newName: "DG");

            migrationBuilder.RenameColumn(
                name: "Scentaurus",
                table: "TB_GIV_FG_ReceivePalletItem",
                newName: "DG");

            migrationBuilder.AddColumn<bool>(
                name: "Group3",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group6",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group8",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group9",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NDG",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Scentaurus",
                table: "TB_GIV_RM_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group3",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group6",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group8",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Group9",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NDG",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Scentaurus",
                table: "TB_GIV_FG_ReceivePallet",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

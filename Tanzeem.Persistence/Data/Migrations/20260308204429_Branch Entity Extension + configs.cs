using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class BranchEntityExtensionconfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Branches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Branches",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Branches",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Branches",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Branches",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Branches",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Branches");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class Transaction_TransactionItemaddandmodify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitCost",
                table: "TransactionItems",
                newName: "UnitPrice");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Transactions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Transactions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceReason",
                table: "Transactions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SourceReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "TransactionItems",
                newName: "UnitCost");
        }
    }
}

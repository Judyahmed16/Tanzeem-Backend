using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class idssuporddel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierNumber",
                table: "Supplier",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Old-Record");

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Order",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Old-Record");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryIssueNumber",
                table: "DeliveryIssues",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Old-Record");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierNumber",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryIssueNumber",
                table: "DeliveryIssues");
        }
    }
}

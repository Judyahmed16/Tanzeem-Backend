using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingnamesinT_TI_TDto_TIDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Transactions",
                newName: "TotalTransactedItems");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "TransactionItems",
                newName: "QuantityOfTransactedItem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalTransactedItems",
                table: "Transactions",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "QuantityOfTransactedItem",
                table: "TransactionItems",
                newName: "Quantity");
        }
    }
}

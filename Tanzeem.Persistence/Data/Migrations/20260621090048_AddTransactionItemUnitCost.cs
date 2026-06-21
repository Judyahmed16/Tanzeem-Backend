using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionItemUnitCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "TransactionItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE ti
                SET UnitCost = CASE
                    WHEN t.Type = 1 THEN ti.UnitPrice
                    ELSE p.CostPrice
                END
                FROM TransactionItems ti
                INNER JOIN Transactions t ON ti.TransactionId = t.Id
                INNER JOIN Products p ON ti.ProductId = p.Id
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "TransactionItems");
        }
    }
}

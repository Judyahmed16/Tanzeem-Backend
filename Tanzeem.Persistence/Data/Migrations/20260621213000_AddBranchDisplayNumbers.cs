using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tanzeem.Persistence.Data.DbContexts;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    [DbContextAttribute(typeof(TanzeemDbContext))]
    [Migration("20260621213000_AddBranchDisplayNumbers")]
    public partial class AddBranchDisplayNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductNumber",
                table: "Inventories",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionNumber",
                table: "Transactions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                WITH NumberedInventories AS (
                    SELECT Id,
                           BranchId,
                           ROW_NUMBER() OVER (PARTITION BY BranchId ORDER BY Id) AS SequenceNumber
                    FROM Inventories
                    WHERE ProductNumber IS NULL OR ProductNumber = ''
                )
                UPDATE inventory
                SET ProductNumber = CONCAT(
                    'B',
                    RIGHT('000' + CAST(numbered.BranchId AS varchar(10)), 3),
                    '-PRD-',
                    RIGHT('0000' + CAST(numbered.SequenceNumber AS varchar(10)), 4)
                )
                FROM Inventories inventory
                INNER JOIN NumberedInventories numbered ON inventory.Id = numbered.Id;
                """);

            migrationBuilder.Sql("""
                WITH NumberedTransactions AS (
                    SELECT Id,
                           BranchId,
                           ROW_NUMBER() OVER (PARTITION BY BranchId ORDER BY CreatedAt, Id) AS SequenceNumber
                    FROM Transactions
                    WHERE TransactionNumber IS NULL OR TransactionNumber = ''
                )
                UPDATE transactionRecord
                SET TransactionNumber = CONCAT(
                    'B',
                    RIGHT('000' + CAST(numbered.BranchId AS varchar(10)), 3),
                    '-TRX-',
                    RIGHT('0000' + CAST(numbered.SequenceNumber AS varchar(10)), 4)
                )
                FROM Transactions transactionRecord
                INNER JOIN NumberedTransactions numbered ON transactionRecord.Id = numbered.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_BranchId_ProductNumber",
                table: "Inventories",
                columns: new[] { "BranchId", "ProductNumber" },
                unique: true,
                filter: "[ProductNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BranchId_TransactionNumber",
                table: "Transactions",
                columns: new[] { "BranchId", "TransactionNumber" },
                unique: true,
                filter: "[TransactionNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_BranchId_ProductNumber",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BranchId_TransactionNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProductNumber",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "Transactions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class transactionAttribAddition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerformedByUserId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "TransactionItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PerformedByUserId",
                table: "Transactions",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_PerformedByUserId",
                table: "Transactions",
                column: "PerformedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_PerformedByUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PerformedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "TransactionItems");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tanzeem.Persistence.Data.DbContexts;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    [DbContextAttribute(typeof(TanzeemDbContext))]
    [Migration("20260622131000_AddSupplierBranchScope")]
    public partial class AddSupplierBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Supplier_Email",
                table: "Supplier");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Supplier",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE supplier
                SET BranchId = branchChoice.BranchId
                FROM Supplier supplier
                OUTER APPLY (
                    SELECT TOP 1 orders.BranchId
                    FROM Orders orders
                    WHERE orders.SupplierId = supplier.Id
                    GROUP BY orders.BranchId
                    ORDER BY COUNT(*) DESC, MIN(orders.Id)
                ) orderBranch
                OUTER APPLY (
                    SELECT TOP 1 branch.Id AS BranchId
                    FROM Branches branch
                    WHERE branch.CompanyId = supplier.CompanyId
                    ORDER BY
                        CASE WHEN branch.Id = orderBranch.BranchId THEN 0 ELSE 1 END,
                        branch.Id
                ) branchChoice
                WHERE supplier.BranchId IS NULL;
                """);

            migrationBuilder.Sql("""
                DELETE supplier
                FROM Supplier supplier
                WHERE supplier.BranchId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Supplier",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_BranchId_Email",
                table: "Supplier",
                columns: new[] { "BranchId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_BranchId",
                table: "Supplier",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Branches_BranchId",
                table: "Supplier",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Branches_BranchId",
                table: "Supplier");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_BranchId_Email",
                table: "Supplier");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_BranchId",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Supplier");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Email",
                table: "Supplier",
                column: "Email",
                unique: true);
        }
    }
}

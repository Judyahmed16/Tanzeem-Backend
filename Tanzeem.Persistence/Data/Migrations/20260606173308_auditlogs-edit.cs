using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class auditlogsedit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AuditTrials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrials_BranchId",
                table: "AuditTrials",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditTrials_Branches_BranchId",
                table: "AuditTrials",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditTrials_Branches_BranchId",
                table: "AuditTrials");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrials_BranchId",
                table: "AuditTrials");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AuditTrials");
        }
    }
}

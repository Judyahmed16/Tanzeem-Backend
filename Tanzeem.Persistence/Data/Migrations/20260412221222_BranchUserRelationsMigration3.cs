using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class BranchUserRelationsMigration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchUserRelationship_Companies_CompanyId",
                table: "BranchUserRelationship");

            migrationBuilder.DropIndex(
                name: "IX_BranchUserRelationship_CompanyId",
                table: "BranchUserRelationship");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BranchUserRelationship");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "BranchUserRelationship",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchUserRelationship_CompanyId",
                table: "BranchUserRelationship",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchUserRelationship_Companies_CompanyId",
                table: "BranchUserRelationship",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}

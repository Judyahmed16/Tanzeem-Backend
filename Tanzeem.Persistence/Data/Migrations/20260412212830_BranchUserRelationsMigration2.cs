using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class BranchUserRelationsMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BranchUserRelationship_UserId",
                table: "BranchUserRelationship",
                column: "UserId",
                unique: true,
                filter: "[IsPrimary] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchUserRelationship_UserId",
                table: "BranchUserRelationship");
        }
    }
}

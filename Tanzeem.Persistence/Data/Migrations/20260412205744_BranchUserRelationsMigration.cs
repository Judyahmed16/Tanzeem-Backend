using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class BranchUserRelationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Companies_CompanyId",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Order_CompanyId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Order");

            migrationBuilder.CreateTable(
                name: "BranchUserRelationship",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchUserRelationship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchUserRelationship_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchUserRelationship_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BranchUserRelationship_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchUserRelationship_BranchId",
                table: "BranchUserRelationship",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchUserRelationship_CompanyId",
                table: "BranchUserRelationship",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchUserRelationship_UserId_BranchId",
                table: "BranchUserRelationship",
                columns: new[] { "UserId", "BranchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchUserRelationship");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Order_CompanyId",
                table: "Order",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Companies_CompanyId",
                table: "Order",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

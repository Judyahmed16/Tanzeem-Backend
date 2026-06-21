using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class categoryrelationcompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Category",
                type: "int",
                nullable: false,
                defaultValue: 84);

            migrationBuilder.CreateIndex(
                name: "IX_Category_CompanyId",
                table: "Category",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Category_Companies_CompanyId",
                table: "Category",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Category_Companies_CompanyId",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Category_CompanyId",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Category");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class editataidb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DemandForecasts_ProductId",
                table: "DemandForecasts");

            migrationBuilder.CreateIndex(
                name: "IX_DemandForecasts_ProductId",
                table: "DemandForecasts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DemandForecasts_ProductId",
                table: "DemandForecasts");

            migrationBuilder.CreateIndex(
                name: "IX_DemandForecasts_ProductId",
                table: "DemandForecasts",
                column: "ProductId",
                unique: true);
        }
    }
}

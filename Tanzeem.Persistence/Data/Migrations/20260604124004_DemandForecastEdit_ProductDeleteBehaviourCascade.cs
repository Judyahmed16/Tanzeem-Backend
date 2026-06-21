using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class DemandForecastEdit_ProductDeleteBehaviourCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandForecasts_Products_ProductId",
                table: "DemandForecasts");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandForecasts_Products_ProductId",
                table: "DemandForecasts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandForecasts_Products_ProductId",
                table: "DemandForecasts");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandForecasts_Products_ProductId",
                table: "DemandForecasts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

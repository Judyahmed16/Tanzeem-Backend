using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class nullablerelationdelsup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryIssues_Supplier_SupplierId",
                table: "DeliveryIssues");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "DeliveryIssues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryIssues_Supplier_SupplierId",
                table: "DeliveryIssues",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryIssues_Supplier_SupplierId",
                table: "DeliveryIssues");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "DeliveryIssues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryIssues_Supplier_SupplierId",
                table: "DeliveryIssues",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

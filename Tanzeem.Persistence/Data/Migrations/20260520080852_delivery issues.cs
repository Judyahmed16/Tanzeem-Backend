using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class deliveryissues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemsAffected = table.Column<int>(type: "int", nullable: false),
                    Discrepancy = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryIssues_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryIssues_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryIssues_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryIssueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    DeliveryIssueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryIssueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryIssueItems_DeliveryIssues_DeliveryIssueId",
                        column: x => x.DeliveryIssueId,
                        principalTable: "DeliveryIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryIssueItems_OrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryIssueItems_DeliveryIssueId",
                table: "DeliveryIssueItems",
                column: "DeliveryIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryIssueItems_OrderItemId",
                table: "DeliveryIssueItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryIssues_BranchId",
                table: "DeliveryIssues",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryIssues_OrderId",
                table: "DeliveryIssues",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryIssues_SupplierId",
                table: "DeliveryIssues",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryIssueItems");

            migrationBuilder.DropTable(
                name: "DeliveryIssues");
        }
    }
}

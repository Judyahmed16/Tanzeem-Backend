using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_BranchId_ProductId_BatchNumber",
                table: "InventoryBatches",
                columns: new[] { "BranchId", "ProductId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductId",
                table: "InventoryBatches",
                column: "ProductId");

            migrationBuilder.Sql("""
                INSERT INTO [InventoryBatches] ([ProductId], [BranchId], [BatchNumber], [Quantity], [ExpiryDate], [CostPrice], [ReceivedAt])
                SELECT
                    i.[ProductId],
                    i.[BranchId],
                    CONCAT(N'LEGACY-', i.[Id]),
                    COALESCE(i.[Quantity], 0),
                    p.[ExpiryDate],
                    p.[CostPrice],
                    SYSUTCDATETIME()
                FROM [Inventories] i
                INNER JOIN [Products] p ON p.[Id] = i.[ProductId]
                WHERE COALESCE(i.[Quantity], 0) > 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [InventoryBatches] b
                      WHERE b.[ProductId] = i.[ProductId]
                        AND b.[BranchId] = i.[BranchId]
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBatches");
        }
    }
}

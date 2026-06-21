using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class alertsConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    DaysBeforeExpiry = table.Column<int>(type: "int", nullable: false),
                    DaysWithoutMovement = table.Column<int>(type: "int", nullable: false),
                    IsActive_InAppNotifiation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_EmailNotifiation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_LowAlert = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_OutAlert = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_ExpiryAlert = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_DeadAlert = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_NewOrderAlert = table.Column<bool>(type: "bit", nullable: false),
                    IsActive_OrderUpdateAlert = table.Column<bool>(type: "bit", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertConfigurations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertConfigurations_BranchId",
                table: "AlertConfigurations",
                column: "BranchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertConfigurations");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class changerelationnotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Notification",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                newName: "IX_Notification_BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Branches_BranchId",
                table: "Notification",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Branches_BranchId",
                table: "Notification");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "Notification",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_BranchId",
                table: "Notification",
                newName: "IX_Notification_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

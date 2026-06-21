using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class subscriptionEditsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "Subscription",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StripeCustomerId",
                table: "Users",
                column: "StripeCustomerId",
                unique: true,
                filter: "[StripeCustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_StripeSubscriptionId",
                table: "Subscription",
                column: "StripeSubscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_StripeCustomerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subscription_StripeSubscriptionId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "Subscription");
        }
    }
}

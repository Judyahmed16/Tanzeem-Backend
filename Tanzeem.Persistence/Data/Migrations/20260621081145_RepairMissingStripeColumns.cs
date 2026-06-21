using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingStripeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Users]', N'StripeCustomerId') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [StripeCustomerId] nvarchar(450) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Subscription]', N'StripeSubscriptionId') IS NULL
                BEGIN
                    ALTER TABLE [Subscription] ADD [StripeSubscriptionId] nvarchar(450) NOT NULL CONSTRAINT [DF_Subscription_StripeSubscriptionId_Repair] DEFAULT N'';
                    ALTER TABLE [Subscription] DROP CONSTRAINT [DF_Subscription_StripeSubscriptionId_Repair];
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Users]', N'StripeCustomerId') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE [name] = N'IX_Users_StripeCustomerId'
                         AND [object_id] = OBJECT_ID(N'[Users]')
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Users_StripeCustomerId]
                    ON [Users] ([StripeCustomerId])
                    WHERE [StripeCustomerId] IS NOT NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Subscription]', N'StripeSubscriptionId') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE [name] = N'IX_Subscription_StripeSubscriptionId'
                         AND [object_id] = OBJECT_ID(N'[Subscription]')
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Subscription_StripeSubscriptionId]
                    ON [Subscription] ([StripeSubscriptionId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: this migration repairs columns/indexes that should
            // already exist from 20260427174453_subscriptionEditsMigration.
        }
    }
}

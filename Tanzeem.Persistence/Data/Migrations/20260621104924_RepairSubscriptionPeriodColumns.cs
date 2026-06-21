using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairSubscriptionPeriodColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Subscription]', N'StartedAt') IS NULL
                BEGIN
                    ALTER TABLE [Subscription] ADD [StartedAt] datetime2 NOT NULL CONSTRAINT [DF_Subscription_StartedAt_Repair] DEFAULT SYSUTCDATETIME();
                    ALTER TABLE [Subscription] DROP CONSTRAINT [DF_Subscription_StartedAt_Repair];
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[Subscription]', N'ExpiresAt') IS NULL
                BEGIN
                    ALTER TABLE [Subscription] ADD [ExpiresAt] datetime2 NOT NULL CONSTRAINT [DF_Subscription_ExpiresAt_Repair] DEFAULT DATEADD(year, 1, SYSUTCDATETIME());
                    ALTER TABLE [Subscription] DROP CONSTRAINT [DF_Subscription_ExpiresAt_Repair];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: this repairs columns the current model expects.
        }
    }
}

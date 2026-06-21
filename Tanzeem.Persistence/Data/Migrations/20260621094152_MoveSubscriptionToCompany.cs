using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveSubscriptionToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[FK_Subscription_Users_UserId]', N'F') IS NOT NULL
                BEGIN
                    ALTER TABLE [Subscription] DROP CONSTRAINT [FK_Subscription_Users_UserId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Subscription_UserId'
                      AND [object_id] = OBJECT_ID(N'[Subscription]')
                )
                BEGIN
                    DROP INDEX [IX_Subscription_UserId] ON [Subscription];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Users_StripeCustomerId'
                      AND [object_id] = OBJECT_ID(N'[Users]')
                )
                BEGIN
                    DROP INDEX [IX_Users_StripeCustomerId] ON [Users];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'CompanyId') IS NULL
                BEGIN
                    ALTER TABLE [Subscription] ADD [CompanyId] int NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'UserId') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE s
                        SET CompanyId = u.CompanyId
                        FROM [Subscription] s
                        INNER JOIN [Users] u ON u.Id = s.UserId
                        WHERE u.CompanyId IS NOT NULL;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                DELETE FROM [Subscription]
                WHERE CompanyId IS NULL
                   OR NOT EXISTS (
                       SELECT 1
                       FROM [Companies] c
                       WHERE c.Id = [Subscription].CompanyId
                   );
                """);

            migrationBuilder.Sql("""
                WITH RankedSubscriptions AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY CompanyId
                               ORDER BY Id DESC
                           ) AS RowNumber
                    FROM [Subscription]
                )
                DELETE FROM RankedSubscriptions
                WHERE RowNumber > 1;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'CompanyId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [Subscription] WHERE [CompanyId] IS NULL)
                BEGIN
                    ALTER TABLE [Subscription] ALTER COLUMN [CompanyId] int NOT NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'UserId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Subscription] DROP COLUMN [UserId];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Users]', N'StripeCustomerId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [StripeCustomerId];
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Subscription_CompanyId'
                      AND [object_id] = OBJECT_ID(N'[Subscription]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Subscription_CompanyId]
                    ON [Subscription] ([CompanyId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[FK_Subscription_Companies_CompanyId]', N'F') IS NULL
                BEGIN
                    ALTER TABLE [Subscription]
                    ADD CONSTRAINT [FK_Subscription_Companies_CompanyId]
                    FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Subscription]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[FK_Subscription_Companies_CompanyId]', N'F') IS NOT NULL
                BEGIN
                    ALTER TABLE [Subscription] DROP CONSTRAINT [FK_Subscription_Companies_CompanyId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Subscription_CompanyId'
                      AND [object_id] = OBJECT_ID(N'[Subscription]')
                )
                BEGIN
                    DROP INDEX [IX_Subscription_CompanyId] ON [Subscription];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'UserId') IS NULL
                BEGIN
                    ALTER TABLE [Subscription] ADD [UserId] int NULL;
                END
                """);

            migrationBuilder.Sql("""
                UPDATE s
                SET UserId = companyUsers.Id
                FROM [Subscription] s
                CROSS APPLY (
                    SELECT TOP 1 u.Id
                    FROM [Users] u
                    WHERE u.CompanyId = s.CompanyId
                    ORDER BY u.Id
                ) companyUsers;
                """);

            migrationBuilder.Sql("""
                DELETE FROM [Subscription]
                WHERE UserId IS NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'UserId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [Subscription] WHERE [UserId] IS NULL)
                BEGIN
                    ALTER TABLE [Subscription] ALTER COLUMN [UserId] int NOT NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Subscription]', N'CompanyId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Subscription] DROP COLUMN [CompanyId];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[Users]', N'StripeCustomerId') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [StripeCustomerId] nvarchar(450) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_Subscription_UserId'
                      AND [object_id] = OBJECT_ID(N'[Subscription]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Subscription_UserId]
                    ON [Subscription] ([UserId]);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
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
                IF OBJECT_ID(N'[FK_Subscription_Users_UserId]', N'F') IS NULL
                BEGIN
                    ALTER TABLE [Subscription]
                    ADD CONSTRAINT [FK_Subscription_Users_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
                END
                """);
        }
    }
}

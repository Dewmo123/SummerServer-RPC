using System.Net;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Tests.Infrastructure.Configuration;

namespace SummerProject.Server.Tests.Infrastructure.Database;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task EmptyDatabaseCreatesEntireSchema()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        SqliteConnectionFactory connectionFactory =
            application.Services.GetRequiredService<SqliteConnectionFactory>();
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync();
        string[] tables = (await connection.QueryAsync<string>("""
            SELECT name
            FROM sqlite_schema
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """)).ToArray();

        Assert.Equal(
            ["characters", "currencies", "refresh_tokens", "schema_migrations", "stage_runs", "user_rooms", "users"],
            tables);

        SchemaMigrationModel migration = await connection.QuerySingleAsync<SchemaMigrationModel>("""
            SELECT version AS Version,
                   name AS Name,
                   checksum AS Checksum,
                   applied_at_utc_ms AS AppliedAtUtcMs
            FROM schema_migrations;
            """);
        Assert.Equal(1, migration.Version);
        Assert.Equal("0001_initial.sql", migration.Name);
        Assert.Equal(64, migration.Checksum.Length);
        Assert.True(migration.AppliedAtUtcMs > 0);
    }

    [Fact]
    public async Task RestartDoesNotApplySameMigrationAgain()
    {
        using SqliteIntegrationTestFixture database = new();

        await using (ConfiguredServerApplicationFactory firstApplication = new(database.DatabasePath))
        {
            using HttpClient client = firstApplication.CreateClient();
            using HttpResponseMessage health = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }

        await using ConfiguredServerApplicationFactory secondApplication = new(database.DatabasePath);
        using HttpClient secondClient = secondApplication.CreateClient();
        using HttpResponseMessage secondHealth = await secondClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, secondHealth.StatusCode);

        SqliteConnectionFactory connectionFactory =
            secondApplication.Services.GetRequiredService<SqliteConnectionFactory>();
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync();
        int migrationCount = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM schema_migrations;");
        Assert.Equal(1, migrationCount);
    }

    [Fact]
    public async Task ChangedMigrationChecksumPreventsRestart()
    {
        using SqliteIntegrationTestFixture database = new();

        await using (ConfiguredServerApplicationFactory firstApplication = new(database.DatabasePath))
        {
            using HttpClient client = firstApplication.CreateClient();
            using HttpResponseMessage health = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);

            SqliteConnectionFactory connectionFactory =
                firstApplication.Services.GetRequiredService<SqliteConnectionFactory>();
            await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync();
            await connection.ExecuteAsync(
                "UPDATE schema_migrations SET checksum = @Checksum WHERE version = 1;",
                new { Checksum = new string('0', 64) });
        }

        await using ConfiguredServerApplicationFactory secondApplication = new(database.DatabasePath);
        Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using HttpClient client = secondApplication.CreateClient();
            await client.GetAsync("/health");
        });

        Assert.Contains("체크섬이 현재 파일과 일치하지 않습니다", exception.ToString(), StringComparison.Ordinal);
    }
}
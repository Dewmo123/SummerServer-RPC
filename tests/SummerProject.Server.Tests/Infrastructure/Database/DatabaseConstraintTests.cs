using System.Net;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Tests.Infrastructure.Configuration;

namespace SummerProject.Server.Tests.Infrastructure.Database;

public sealed class DatabaseConstraintTests
{
    [Fact]
    public async Task ForeignKeysRejectOrphansAndCascadeUserRows()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        await using SqliteConnection connection = await OpenConnectionAsync(application);

        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO characters (user_id, level, exp) VALUES (999, 1, 0);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO currencies (user_id, type, amount) VALUES (999, 1, 0);");
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000001', 999,
                 '10000000-0000-0000-0000-000000000001', zeroblob(32), 1, 2);
            """);
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms) VALUES (999, 1, 0, 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (999, 1, '[]', 1);");

        await InsertUserAsync(connection, 1, "user-one", 1, "provider-one");
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms, replaced_by_token_id)
            VALUES
                ('00000000-0000-0000-0000-000000000002', 1,
                 '10000000-0000-0000-0000-000000000002', zeroblob(32), 1, 2,
                 '00000000-0000-0000-0000-000000000099');
            """);

        await connection.ExecuteAsync("INSERT INTO characters (user_id, level, exp) VALUES (1, 1, 0);");
        await connection.ExecuteAsync("INSERT INTO currencies (user_id, type, amount) VALUES (1, 1, 0);");
        await connection.ExecuteAsync("""
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000003', 1,
                 '10000000-0000-0000-0000-000000000003', zeroblob(32), 1, 2);
            """);
        await connection.ExecuteAsync(
            "INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms) VALUES (1, 1, 0, 1);");
        await connection.ExecuteAsync(
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (1, 1, '[]', 1);");

        await connection.ExecuteAsync("DELETE FROM users WHERE id = 1;");
        int relatedRowCount = await connection.QuerySingleAsync<int>("""
            SELECT (SELECT COUNT(*) FROM characters)
                 + (SELECT COUNT(*) FROM currencies)
                 + (SELECT COUNT(*) FROM refresh_tokens)
                 + (SELECT COUNT(*) FROM stage_runs)
                 + (SELECT COUNT(*) FROM user_rooms);
            """);
        Assert.Equal(0, relatedRowCount);
    }

    [Fact]
    public async Task UniqueConstraintsRejectDuplicateBusinessKeys()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        await using SqliteConnection connection = await OpenConnectionAsync(application);

        await InsertUserAsync(connection, 1, "user-one", 1, "provider-one");
        await InsertUserAsync(connection, 2, "user-two", 1, "provider-two");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms) VALUES ('user-one', 1, 'provider-three', 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms) VALUES ('user-three', 1, 'provider-one', 1);");

        await connection.ExecuteAsync("INSERT INTO characters (user_id, level, exp) VALUES (1, 1, 0);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO characters (user_id, level, exp) VALUES (1, 2, 0);");
        await connection.ExecuteAsync("INSERT INTO currencies (user_id, type, amount) VALUES (1, 1, 0);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO currencies (user_id, type, amount) VALUES (1, 1, 10);");
        await connection.ExecuteAsync(
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (1, 1, '[]', 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (1, 2, '[]', 2);");

        await connection.ExecuteAsync(
            "INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms) VALUES (1, 1, 0, 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms) VALUES (1, 2, 0, 2);");

        await connection.ExecuteAsync("""
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000010', 1,
                 '10000000-0000-0000-0000-000000000010', zeroblob(32), 1, 2);
            """);
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000011', 2,
                 '10000000-0000-0000-0000-000000000011', zeroblob(32), 1, 2);
            """);
    }

    [Fact]
    public async Task CheckConstraintsRejectInvalidStoredValues()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        await using SqliteConnection connection = await OpenConnectionAsync(application);

        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO schema_migrations (version, name, checksum, applied_at_utc_ms) VALUES (0, 'invalid', @Checksum, 1);",
            new { Checksum = new string('0', 64) });
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO schema_migrations (version, name, checksum, applied_at_utc_ms) VALUES (2, '', @Checksum, 1);",
            new { Checksum = new string('0', 64) });
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO schema_migrations (version, name, checksum, applied_at_utc_ms) VALUES (2, 'invalid', 'short', 1);");

        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms) VALUES ('', 1, 'provider-a', 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms) VALUES ('user-a', 0, 'provider-a', 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms) VALUES ('user-a', 1, '', 1);");

        await InsertUserAsync(connection, 1, "user-one", 1, "provider-one");
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES ('short', 1, '10000000-0000-0000-0000-000000000020', zeroblob(32), 1, 2);
            """);
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000021', 1,
                 'short', zeroblob(32), 1, 2);
            """);
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms)
            VALUES
                ('00000000-0000-0000-0000-000000000022', 1,
                 '10000000-0000-0000-0000-000000000022', zeroblob(31), 1, 2);
            """);
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO refresh_tokens
                (id, user_id, family_id, token_hash, created_at_utc_ms, expires_at_utc_ms, revoke_reason)
            VALUES
                ('00000000-0000-0000-0000-000000000023', 1,
                 '10000000-0000-0000-0000-000000000023', zeroblob(32), 1, 2, @Reason);
            """, new { Reason = new string('r', 65) });

        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO characters (user_id, level, exp) VALUES (1, 0, 0);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO characters (user_id, level, exp) VALUES (1, 1, -1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO currencies (user_id, type, amount) VALUES (1, 5, 0);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO currencies (user_id, type, amount) VALUES (1, 1, -1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms) VALUES (1, 1, 3, 1);");
        await AssertConstraintViolationAsync(connection, """
            INSERT INTO stage_runs
                (user_id, stage_id, status, started_at_utc_ms, exp_gained)
            VALUES (1, 1, 1, 1, -1);
            """);
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (1, 1, 'not-json', 1);");
        await AssertConstraintViolationAsync(
            connection,
            "INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms) VALUES (1, 1, '{}', 1);");
    }

    private static async Task<SqliteConnection> OpenConnectionAsync(
        ConfiguredServerApplicationFactory application)
    {
        using HttpClient client = application.CreateClient();
        using HttpResponseMessage health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        SqliteConnectionFactory connectionFactory =
            application.Services.GetRequiredService<SqliteConnectionFactory>();
        return await connectionFactory.OpenConnectionAsync();
    }

    private static Task InsertUserAsync(
        SqliteConnection connection,
        long id,
        string username,
        int provider,
        string providerUserId) =>
        connection.ExecuteAsync("""
            INSERT INTO users (id, username, provider, provider_user_id, created_at_utc_ms)
            VALUES (@Id, @Username, @Provider, @ProviderUserId, 1);
            """, new { Id = id, Username = username, Provider = provider, ProviderUserId = providerUserId });

    private static async Task AssertConstraintViolationAsync(
        SqliteConnection connection,
        string sql,
        object? parameters = null)
    {
        SqliteException exception = await Assert.ThrowsAsync<SqliteException>(
            () => connection.ExecuteAsync(sql, parameters));
        Assert.Equal(19, exception.SqliteErrorCode);
    }
}
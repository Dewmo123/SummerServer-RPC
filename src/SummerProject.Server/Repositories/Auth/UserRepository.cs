using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;

namespace SummerProject.Server.Repositories.Auth;

internal sealed class UserRepository(SqliteConnectionFactory connectionFactory)
{
    private const string SelectColumns = """
        id AS Id,
        username AS Username,
        provider AS Provider,
        provider_user_id AS ProviderUserId,
        created_at_utc_ms AS CreatedAtUtcMs
        """;

    public async ValueTask<UserModel?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserModel>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM users WHERE username = @Username;",
            new { Username = username },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<UserModel?> FindByIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserModel>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM users WHERE id = @UserId;",
            new { UserId = userId },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<UserModel?> GetOrCreateGoogleUserAsync(
        string providerUserId,
        string username,
        long createdAtUtcMs,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        // 즉시 쓰기 잠금을 잡아 같은 Google 사용자의 최초 로그인이 한 행만 생성하도록 직렬화한다.
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT OR IGNORE INTO users (username, provider, provider_user_id, created_at_utc_ms)
            VALUES (@Username, @Provider, @ProviderUserId, @CreatedAtUtcMs);
            """,
            new
            {
                Username = username,
                Provider = LoginProviderProto.Google,
                ProviderUserId = providerUserId,
                CreatedAtUtcMs = createdAtUtcMs
            },
            transaction,
            cancellationToken: cancellationToken));

        UserModel? user = await connection.QuerySingleOrDefaultAsync<UserModel>(new CommandDefinition(
            $"""
            SELECT {SelectColumns}
            FROM users
            WHERE provider = @Provider AND provider_user_id = @ProviderUserId;
            """,
            new
            {
                Provider = LoginProviderProto.Google,
                ProviderUserId = providerUserId
            },
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return user;
    }
}
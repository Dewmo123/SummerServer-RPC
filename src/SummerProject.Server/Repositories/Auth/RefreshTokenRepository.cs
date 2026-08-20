using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;

namespace SummerProject.Server.Repositories.Auth;

internal enum RefreshRotationStatus
{
    Succeeded,
    Invalid,
    Reused
}

internal sealed record RefreshRotationResult(
    RefreshRotationStatus Status,
    RefreshTokenModel? PreviousToken = null,
    IssuedRefreshTokenProto? ReplacementToken = null);

internal sealed class RefreshTokenRepository(SqliteConnectionFactory connectionFactory)
{
    private const string SelectTokenSql = """
        SELECT refresh_tokens.id AS Id,
               refresh_tokens.user_id AS UserId,
               refresh_tokens.family_id AS FamilyId,
               refresh_tokens.token_hash AS TokenHash,
               refresh_tokens.created_at_utc_ms AS CreatedAtUtcMs,
               refresh_tokens.expires_at_utc_ms AS ExpiresAtUtcMs,
               refresh_tokens.used_at_utc_ms AS UsedAtUtcMs,
               refresh_tokens.revoked_at_utc_ms AS RevokedAtUtcMs,
               refresh_tokens.revoke_reason AS RevokeReason,
               refresh_tokens.replaced_by_token_id AS ReplacedByTokenId,
               users.username AS Username,
               users.provider AS Provider
        FROM refresh_tokens
        INNER JOIN users ON users.id = refresh_tokens.user_id
        WHERE refresh_tokens.token_hash = @TokenHash;
        """;

    private const string InsertTokenSql = """
        INSERT INTO refresh_tokens (
            id,
            user_id,
            family_id,
            token_hash,
            created_at_utc_ms,
            expires_at_utc_ms)
        VALUES (
            @Id,
            @UserId,
            @FamilyId,
            @Hash,
            @CreatedAtUtcMs,
            @ExpiresAtUtcMs);
        """;

    public async ValueTask InsertAsync(
        long userId,
        IssuedRefreshTokenProto token,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await InsertAsync(connection, null, userId, token, cancellationToken);
    }

    public async ValueTask<RefreshRotationResult> RotateAsync(
        byte[] currentTokenHash,
        Func<string, DateTimeOffset, IssuedRefreshTokenProto> replacementFactory,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        // 회전 판정과 후속 토큰 삽입 사이에 다른 쓰기가 끼어들지 않도록 즉시 트랜잭션을 사용한다.
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        RefreshTokenModel? current = await FindAsync(
            connection,
            transaction,
            currentTokenHash,
            cancellationToken);
        long nowUtcMs = now.ToUnixTimeMilliseconds();

        if (current is null
            || current.ExpiresAtUtcMs <= nowUtcMs
            || current.RevokedAtUtcMs is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);
        }

        if (current.UsedAtUtcMs is not null)
        {
            await RevokeFamilyAsync(
                connection,
                transaction,
                current.FamilyId,
                nowUtcMs,
                "reuse_detected",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefreshRotationResult(RefreshRotationStatus.Reused);
        }

        DateTimeOffset absoluteExpiration = DateTimeOffset.FromUnixTimeMilliseconds(current.ExpiresAtUtcMs);
        IssuedRefreshTokenProto replacement = replacementFactory(current.FamilyId, absoluteExpiration);
        await InsertAsync(connection, transaction, current.UserId, replacement, cancellationToken);

        int claimed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE refresh_tokens
            SET used_at_utc_ms = @UsedAtUtcMs,
                replaced_by_token_id = @ReplacementId
            WHERE id = @Id
              AND used_at_utc_ms IS NULL
              AND revoked_at_utc_ms IS NULL
              AND expires_at_utc_ms > @UsedAtUtcMs;
            """,
            new
            {
                UsedAtUtcMs = nowUtcMs,
                ReplacementId = replacement.Id,
                current.Id
            },
            transaction,
            cancellationToken: cancellationToken));

        if (claimed != 1)
        {
            // 선점 실패는 탈취 가능성을 구분할 수 없으므로 새 토큰까지 포함해 패밀리를 폐기한다.
            await RevokeFamilyAsync(
                connection,
                transaction,
                current.FamilyId,
                nowUtcMs,
                "reuse_detected",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefreshRotationResult(RefreshRotationStatus.Reused);
        }

        await transaction.CommitAsync(cancellationToken);
        return new RefreshRotationResult(RefreshRotationStatus.Succeeded, current, replacement);
    }

    public async ValueTask RevokeFamilyByTokenHashAsync(
        byte[] tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        RefreshTokenModel? token = await FindAsync(
            connection,
            transaction,
            tokenHash,
            cancellationToken);

        if (token is not null)
        {
            // 존재 여부나 기존 폐기 상태를 외부에 드러내지 않고 같은 성공 결과를 반환한다.
            await RevokeFamilyAsync(
                connection,
                transaction,
                token.FamilyId,
                now.ToUnixTimeMilliseconds(),
                "logout",
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async ValueTask<RefreshTokenModel?> FindAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        byte[] tokenHash,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<RefreshTokenModel>(new CommandDefinition(
            SelectTokenSql,
            new { TokenHash = tokenHash },
            transaction,
            cancellationToken: cancellationToken));

    private static async ValueTask InsertAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        long userId,
        IssuedRefreshTokenProto token,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            InsertTokenSql,
            new
            {
                token.Id,
                UserId = userId,
                token.FamilyId,
                token.Hash,
                CreatedAtUtcMs = token.CreatedAt.ToUnixTimeMilliseconds(),
                ExpiresAtUtcMs = token.ExpiresAt.ToUnixTimeMilliseconds()
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async ValueTask RevokeFamilyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string familyId,
        long revokedAtUtcMs,
        string reason,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE refresh_tokens
            SET revoked_at_utc_ms = @RevokedAtUtcMs,
                revoke_reason = @Reason
            WHERE family_id = @FamilyId
              AND revoked_at_utc_ms IS NULL;
            """,
            new
            {
                RevokedAtUtcMs = revokedAtUtcMs,
                Reason = reason,
                FamilyId = familyId
            },
            transaction,
            cancellationToken: cancellationToken));
    }
}
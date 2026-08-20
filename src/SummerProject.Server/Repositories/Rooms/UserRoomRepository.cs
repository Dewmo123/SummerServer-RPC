using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Datas.Rooms;

namespace SummerProject.Server.Repositories.Rooms;

internal enum UserRoomUpsertStatus
{
    Succeeded,
    UserNotFound
}

/// <summary>
/// user_rooms의 전체 스냅샷 Upsert와 사용자 기준 조회 SQL을 담당합니다.
/// </summary>
internal sealed class UserRoomRepository(SqliteConnectionFactory connectionFactory)
{
    public async ValueTask<UserRoomUpsertStatus> UpsertAsync(
        long userId,
        long mapId,
        string trapsJson,
        long updatedAtUtcMs,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);

        bool userExists = await connection.QuerySingleAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE id = @UserId);",
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));
        if (!userExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return UserRoomUpsertStatus.UserNotFound;
        }

        // 존재 확인과 전체 스냅샷 교체를 같은 쓰기 트랜잭션에 두어 사용자 삭제 경쟁과 부분 갱신을 막는다.
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms)
            VALUES (@UserId, @MapId, @TrapsJson, @UpdatedAtUtcMs)
            ON CONFLICT(user_id) DO UPDATE SET
                map_id = excluded.map_id,
                traps_json = excluded.traps_json,
                updated_at_utc_ms = excluded.updated_at_utc_ms;
            """,
            new
            {
                UserId = userId,
                MapId = mapId,
                TrapsJson = trapsJson,
                UpdatedAtUtcMs = updatedAtUtcMs
            },
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return UserRoomUpsertStatus.Succeeded;
    }

    public async ValueTask<UserRoomModel?> FindByUserIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserRoomModel>(new CommandDefinition(
            """
            SELECT user_id AS UserId,
                   map_id AS MapId,
                   traps_json AS TrapsJson,
                   updated_at_utc_ms AS UpdatedAtUtcMs
            FROM user_rooms
            WHERE user_id = @UserId;
            """,
            new { UserId = userId },
            cancellationToken: cancellationToken));
    }
}
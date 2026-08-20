using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Datas.Stages;
using SummerProject.Server.Models.Stages;

namespace SummerProject.Server.Repositories.Stages;

internal enum StageEntryRepositoryStatus
{
    Succeeded,
    UserNotFound
}

internal sealed record StageEntryRepositoryResult(
    StageEntryRepositoryStatus Status,
    StageRunModel? Run = null);

internal sealed class StageRunRepository(SqliteConnectionFactory connectionFactory)
{
    private const string SelectRunSql = """
        SELECT id AS Id,
               user_id AS UserId,
               stage_id AS StageId,
               status AS Status,
               started_at_utc_ms AS StartedAtUtcMs,
               completed_at_utc_ms AS CompletedAtUtcMs,
               exp_gained AS ExpGained,
               currencies_gained_json AS CurrenciesGainedJson
        FROM stage_runs
        WHERE id = @RunId;
        """;

    public async ValueTask<StageEntryRepositoryResult> EnterAsync(
        long userId,
        long stageId,
        long nowUtcMs,
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
            return new StageEntryRepositoryResult(StageEntryRepositoryStatus.UserNotFound);
        }

        // 기존 실행 포기와 새 실행 생성을 한 쓰기 트랜잭션으로 묶어 진행 중 행을 최대 하나로 유지한다.
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE stage_runs
            SET status = @AbandonedStatus,
                completed_at_utc_ms = @NowUtcMs
            WHERE user_id = @UserId
              AND status = @InProgressStatus;
            """,
            new
            {
                UserId = userId,
                NowUtcMs = nowUtcMs,
                AbandonedStatus = StageRunStatusProto.Abandoned,
                InProgressStatus = StageRunStatusProto.InProgress
            },
            transaction,
            cancellationToken: cancellationToken));

        long runId = await connection.QuerySingleAsync<long>(new CommandDefinition(
            """
            INSERT INTO stage_runs (
                user_id,
                stage_id,
                status,
                started_at_utc_ms,
                completed_at_utc_ms,
                exp_gained,
                currencies_gained_json)
            VALUES (
                @UserId,
                @StageId,
                @Status,
                @NowUtcMs,
                NULL,
                0,
                NULL)
            RETURNING id;
            """,
            new
            {
                UserId = userId,
                StageId = stageId,
                Status = StageRunStatusProto.InProgress,
                NowUtcMs = nowUtcMs
            },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return new StageEntryRepositoryResult(
            StageEntryRepositoryStatus.Succeeded,
            new StageRunModel
            {
                Id = runId,
                UserId = userId,
                StageId = stageId,
                Status = StageRunStatusProto.InProgress,
                StartedAtUtcMs = nowUtcMs,
                ExpGained = 0
            });
    }

    internal static async ValueTask<StageRunModel?> FindInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long runId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<StageRunModel>(new CommandDefinition(
            SelectRunSql,
            new { RunId = runId },
            transaction,
            cancellationToken: cancellationToken));

    internal static async ValueTask<bool> TryCompleteInTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long runId,
        long userId,
        long completedAtUtcMs,
        long expGained,
        string currenciesGainedJson,
        CancellationToken cancellationToken)
    {
        // 소유자와 InProgress 상태를 조건에 포함해 동시 완료 중 한 요청만 보상 권한을 선점한다.
        int changed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE stage_runs
            SET status = @CompletedStatus,
                completed_at_utc_ms = @CompletedAtUtcMs,
                exp_gained = @ExpGained,
                currencies_gained_json = @CurrenciesGainedJson
            WHERE id = @RunId
              AND user_id = @UserId
              AND status = @InProgressStatus;
            """,
            new
            {
                RunId = runId,
                UserId = userId,
                CompletedAtUtcMs = completedAtUtcMs,
                ExpGained = expGained,
                CurrenciesGainedJson = currenciesGainedJson,
                CompletedStatus = StageRunStatusProto.Completed,
                InProgressStatus = StageRunStatusProto.InProgress
            },
            transaction,
            cancellationToken: cancellationToken));
        return changed == 1;
    }
}
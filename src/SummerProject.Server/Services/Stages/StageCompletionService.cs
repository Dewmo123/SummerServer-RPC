using System.Numerics;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Exceptions.Stages;
using SummerProject.Server.Helpers.Stages;
using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Characters;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Models.Datas.Stages;
using SummerProject.Server.Models.GameData;
using SummerProject.Server.Models.Stages;
using SummerProject.Server.Repositories.Stages;
using SummerProject.Server.Services.Characters;
using SummerProject.Server.Services.Currencies;

namespace SummerProject.Server.Services.Stages;

internal sealed class StageCompletionService(
    SqliteConnectionFactory connectionFactory,
    StageCatalogQueryService stageCatalogQueryService,
    CharacterQueryService characterQueryService,
    CharacterProgressionService characterProgressionService,
    CurrencyQueryService currencyQueryService,
    CurrencyBalanceService currencyBalanceService,
    StageRewardSnapshotSerializer rewardSnapshotSerializer,
    TimeProvider timeProvider)
{
    public async ValueTask<StageCompletionProto> CompleteAsync(
        long userId,
        long runId,
        CancellationToken cancellationToken)
    {
        long nowUtcMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        StageRunModel run = await FindAndValidateRunAsync(
            connection,
            transaction,
            userId,
            runId,
            cancellationToken);
        StageProto stage = stageCatalogQueryService.Get(run.StageId);
        EnsureMinimumClearTime(run, stage, nowUtcMs);

        CurrencyProto[] gainedCurrencies =
        [
            new(CurrencyTypeProto.Gold, stage.RewardGold)
        ];
        string rewardSnapshot = rewardSnapshotSerializer.Serialize(gainedCurrencies);
        bool claimed = await StageRunRepository.TryCompleteInTransactionAsync(
            connection,
            transaction,
            run.Id,
            userId,
            nowUtcMs,
            stage.RewardExp,
            rewardSnapshot,
            cancellationToken);
        if (!claimed)
        {
            throw new StageRunAlreadyCompletedException();
        }

        CharacterProto character;
        IReadOnlyList<CurrencyProto> allCurrencies;
        try
        {
            if (stage.RewardGold > 0)
            {
                _ = await currencyBalanceService.IncreaseInTransactionAsync(
                    connection,
                    transaction,
                    userId,
                    CurrencyTypeProto.Gold,
                    stage.RewardGold,
                    cancellationToken);
            }

            character = stage.RewardExp > 0
                ? await characterProgressionService.AddExperienceInTransactionAsync(
                    connection,
                    transaction,
                    userId,
                    stage.RewardExp,
                    cancellationToken)
                : await characterQueryService.GetMineInTransactionAsync(
                    connection,
                    transaction,
                    userId,
                    cancellationToken);
            allCurrencies = await currencyQueryService.ListMineInTransactionAsync(
                connection,
                transaction,
                userId,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            // 완료 상태와 일부 보상이 분리되지 않도록 지급 단계의 모든 실패를 같은 트랜잭션에서 취소한다.
            await transaction.RollbackAsync(CancellationToken.None);
            throw new StageRewardFailedException(exception);
        }

        await transaction.CommitAsync(cancellationToken);
        return new StageCompletionProto(
            stage.StageId,
            stage.RewardExp,
            character,
            gainedCurrencies,
            allCurrencies);
    }

    private static async ValueTask<StageRunModel> FindAndValidateRunAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long userId,
        long runId,
        CancellationToken cancellationToken)
    {
        StageRunModel? run = await StageRunRepository.FindInTransactionAsync(
            connection,
            transaction,
            runId,
            cancellationToken);
        if (run is null)
        {
            throw new StageRunNotFoundException();
        }

        if (run.UserId != userId)
        {
            throw new StageRunForbiddenException();
        }

        if (run.Status != StageRunStatusProto.InProgress)
        {
            throw new StageRunAlreadyCompletedException();
        }

        return run;
    }

    private static void EnsureMinimumClearTime(
        StageRunModel run,
        StageProto stage,
        long nowUtcMs)
    {
        BigInteger elapsedMilliseconds = (BigInteger)nowUtcMs - run.StartedAtUtcMs;
        BigInteger requiredMilliseconds = (BigInteger)stage.MinimumClearSeconds * 1000;
        if (elapsedMilliseconds < requiredMilliseconds)
        {
            throw new StageClearTooEarlyException();
        }
    }
}
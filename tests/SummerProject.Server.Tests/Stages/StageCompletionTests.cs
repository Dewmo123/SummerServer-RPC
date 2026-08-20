using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Stages;

public sealed class StageCompletionTests
{
    [Fact]
    public async Task CompletionAtExactMinimumTimeCommitsRewardSnapshotAndFullState()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-complete-user");
        long runId = await StageTestSupport.EnterAsync(client, session.AccessToken);

        timeProvider.Advance(TimeSpan.FromMilliseconds(999));
        using JsonDocument early = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            session.AccessToken);
        AuthRpcClient.HasError(early, 1405, "STAGE_CLEAR_TOO_EARLY");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            Assert.Equal(0L, await connection.QuerySingleAsync<long>(
                "SELECT status FROM stage_runs WHERE id = @RunId;",
                new { RunId = runId }));
            Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM characters;"));
            Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM currencies;"));
        }

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        using JsonDocument completed = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            session.AccessToken);
        JsonElement result = completed.RootElement.GetProperty("result");
        Assert.Equal(1L, result.GetProperty("stageId").GetInt64());
        Assert.Equal(10L, result.GetProperty("expGained").GetInt64());
        Assert.Equal(1L, result.GetProperty("character").GetProperty("level").GetInt64());
        Assert.Equal(10L, result.GetProperty("character").GetProperty("exp").GetInt64());
        JsonElement.ArrayEnumerator gained = result.GetProperty("gainedCurrencies").EnumerateArray();
        JsonElement gainedGold = Assert.Single(gained);
        Assert.Equal(1, gainedGold.GetProperty("type").GetInt32());
        Assert.Equal(100L, gainedGold.GetProperty("amount").GetInt64());
        JsonElement[] allCurrencies = result.GetProperty("allCurrencies").EnumerateArray().ToArray();
        Assert.Equal([1, 2, 3, 4], allCurrencies.Select(item => item.GetProperty("type").GetInt32()));
        Assert.Equal(100L, allCurrencies[0].GetProperty("amount").GetInt64());
        Assert.All(allCurrencies[1..], item => Assert.Equal(0L, item.GetProperty("amount").GetInt64()));

        using JsonDocument repeated = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            session.AccessToken);
        AuthRpcClient.HasError(repeated, 1404, "STAGE_RUN_ALREADY_COMPLETED");

        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        dynamic run = await verification.QuerySingleAsync(
            """
            SELECT status, exp_gained, currencies_gained_json
            FROM stage_runs
            WHERE id = @RunId;
            """,
            new { RunId = runId });
        Assert.Equal(1L, (long)run.status);
        Assert.Equal(10L, (long)run.exp_gained);
        using JsonDocument snapshot = JsonDocument.Parse((string)run.currencies_gained_json);
        JsonElement snapshotGold = Assert.Single(snapshot.RootElement.EnumerateArray());
        Assert.Equal(1, snapshotGold.GetProperty("type").GetInt32());
        Assert.Equal(100L, snapshotGold.GetProperty("amount").GetInt64());
        Assert.Equal(10L, await verification.QuerySingleAsync<long>(
            "SELECT exp FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
        Assert.Equal(100L, await verification.QuerySingleAsync<long>(
            "SELECT amount FROM currencies WHERE user_id = @UserId AND type = 1;",
            new { session.UserId }));
    }

    [Fact]
    public async Task MissingAndForeignRunReturnDistinctErrorsWithoutChanges()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession owner = await GameplayTestSupport.LoginAsync(client, "stage-run-owner");
        GameplayTestSession other = await GameplayTestSupport.LoginAsync(client, "stage-run-other");
        long runId = await StageTestSupport.EnterAsync(client, owner.AccessToken);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        using JsonDocument missing = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId = long.MaxValue },
            owner.AccessToken);
        using JsonDocument forbidden = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            other.AccessToken);

        AuthRpcClient.HasError(missing, 1402, "STAGE_RUN_NOT_FOUND");
        AuthRpcClient.HasError(forbidden, 1403, "STAGE_RUN_FORBIDDEN");
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>(
            "SELECT status FROM stage_runs WHERE id = @RunId;",
            new { RunId = runId }));
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM characters;"));
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM currencies;"));
    }

    [Fact]
    public async Task ConcurrentCompletionPaysRewardExactlyOnce()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-complete-race-user");
        long runId = await StageTestSupport.EnterAsync(client, session.AccessToken);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        JsonDocument[] responses = await Task.WhenAll(
            AuthRpcClient.PostAsync(client, "stage.complete", new { runId }, session.AccessToken),
            AuthRpcClient.PostAsync(client, "stage.complete", new { runId }, session.AccessToken));
        try
        {
            Assert.Single(responses, response => response.RootElement.TryGetProperty("result", out _));
            JsonDocument loser = Assert.Single(
                responses,
                response => response.RootElement.TryGetProperty("error", out _));
            AuthRpcClient.HasError(loser, 1404, "STAGE_RUN_ALREADY_COMPLETED");
        }
        finally
        {
            foreach (JsonDocument response in responses)
            {
                response.Dispose();
            }
        }

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(1L, await connection.QuerySingleAsync<long>(
            "SELECT status FROM stage_runs WHERE id = @RunId;",
            new { RunId = runId }));
        Assert.Equal(10L, await connection.QuerySingleAsync<long>(
            "SELECT exp FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
        Assert.Equal(100L, await connection.QuerySingleAsync<long>(
            "SELECT amount FROM currencies WHERE user_id = @UserId AND type = 1;",
            new { session.UserId }));
    }

    [Fact]
    public async Task RewardFailureRollsBackRunCharacterAndCurrencies()
    {
        const long level = long.MaxValue / 100;
        const long exp = (level * 100) - 1;
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-reward-failure-user");
        long runId = await StageTestSupport.EnterAsync(client, session.AccessToken);
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "INSERT INTO characters (user_id, level, exp) VALUES (@UserId, @Level, @Exp);",
                new { session.UserId, Level = level, Exp = exp });
        }
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            session.AccessToken);

        AuthRpcClient.HasError(document, 1406, "STAGE_REWARD_FAILED");
        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        dynamic run = await verification.QuerySingleAsync(
            """
            SELECT status, completed_at_utc_ms, exp_gained, currencies_gained_json
            FROM stage_runs
            WHERE id = @RunId;
            """,
            new { RunId = runId });
        Assert.Equal(0L, (long)run.status);
        Assert.Null((long?)run.completed_at_utc_ms);
        Assert.Equal(0L, (long)run.exp_gained);
        Assert.Null((string?)run.currencies_gained_json);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM currencies;"));
        dynamic character = await verification.QuerySingleAsync(
            "SELECT level, exp FROM characters WHERE user_id = @UserId;",
            new { session.UserId });
        Assert.Equal(level, (long)character.level);
        Assert.Equal(exp, (long)character.exp);
    }

    [Fact]
    public async Task RunWithMissingCatalogStageDoesNotChangeState()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-missing-catalog-user");
        long runId;
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            runId = await connection.QuerySingleAsync<long>(
                """
                INSERT INTO stage_runs (user_id, stage_id, status, started_at_utc_ms)
                VALUES (@UserId, 999, 0, @StartedAtUtcMs)
                RETURNING id;
                """,
                new
                {
                    session.UserId,
                    StartedAtUtcMs = timeProvider.GetUtcNow().AddSeconds(-2).ToUnixTimeMilliseconds()
                });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "stage.complete",
            new { runId },
            session.AccessToken);

        AuthRpcClient.HasError(document, 1401, "STAGE_NOT_FOUND");
        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>(
            "SELECT status FROM stage_runs WHERE id = @RunId;",
            new { RunId = runId }));
        Assert.Equal(0L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM characters;"));
        Assert.Equal(0L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM currencies;"));
    }
}
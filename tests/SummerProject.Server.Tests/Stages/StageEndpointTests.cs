using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Stages;

public sealed class StageEndpointTests
{
    [Fact]
    public async Task GetStageIsPublicAndReturnsValidatedCatalog()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "stage.get",
            new { stageId = 1 });

        JsonElement stage = document.RootElement.GetProperty("result").GetProperty("stage");
        Assert.Equal(1L, stage.GetProperty("stageId").GetInt64());
        Assert.Equal(16, stage.GetProperty("width").GetInt32());
        Assert.Equal(8, stage.GetProperty("height").GetInt32());
        Assert.Equal(2, stage.GetProperty("tiles").GetArrayLength());
        Assert.Single(stage.GetProperty("traps").EnumerateArray());
        Assert.Equal(1, stage.GetProperty("minimumClearSeconds").GetInt32());
        Assert.Equal(10L, stage.GetProperty("rewardExp").GetInt64());
        Assert.Equal(100L, stage.GetProperty("rewardGold").GetInt64());
    }

    [Fact]
    public async Task MissingStageReturnsStableErrorForGetAndEnter()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "missing-stage-user");

        using JsonDocument get = await AuthRpcClient.PostAsync(
            client,
            "stage.get",
            new { stageId = 999 });
        using JsonDocument enter = await AuthRpcClient.PostAsync(
            client,
            "stage.enter",
            new { stageId = 999 },
            session.AccessToken);

        AuthRpcClient.HasError(get, 1401, "STAGE_NOT_FOUND");
        AuthRpcClient.HasError(enter, 1401, "STAGE_NOT_FOUND");
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM stage_runs;"));
    }

    [Theory]
    [InlineData("stage.enter", "stageId")]
    [InlineData("stage.complete", "runId")]
    public async Task StageMutationRequiresAuthentication(string method, string parameterName)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        Dictionary<string, long> parameters = new(StringComparer.Ordinal)
        {
            [parameterName] = 1
        };

        using JsonDocument document = await AuthRpcClient.PostAsync(client, method, parameters);

        AuthRpcClient.HasError(document, -32001, "AUTH_UNAUTHENTICATED");
    }

    [Fact]
    public async Task EnterAbandonsExistingRunAndCreatesNewRunInOneTransaction()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "stage-enter-user");

        long firstRunId = await StageTestSupport.EnterAsync(client, session.AccessToken);
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        long secondRunId = await StageTestSupport.EnterAsync(client, session.AccessToken);

        Assert.NotEqual(firstRunId, secondRunId);
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        dynamic first = await connection.QuerySingleAsync(
            "SELECT status, completed_at_utc_ms FROM stage_runs WHERE id = @RunId;",
            new { RunId = firstRunId });
        dynamic second = await connection.QuerySingleAsync(
            "SELECT status, completed_at_utc_ms FROM stage_runs WHERE id = @RunId;",
            new { RunId = secondRunId });
        Assert.Equal(2L, (long)first.status);
        Assert.NotNull((long?)first.completed_at_utc_ms);
        Assert.Equal(0L, (long)second.status);
        Assert.Null((long?)second.completed_at_utc_ms);
    }

    [Fact]
    public async Task ConcurrentEnterLeavesExactlyOneInProgressRun()
    {
        MutableTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        await using AuthenticationTestApplicationFactory application = new(timeProvider: timeProvider);
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-enter-race-user");

        JsonDocument[] responses = await Task.WhenAll(
            AuthRpcClient.PostAsync(client, "stage.enter", new { stageId = 1 }, session.AccessToken),
            AuthRpcClient.PostAsync(client, "stage.enter", new { stageId = 1 }, session.AccessToken));
        try
        {
            long[] runIds = responses
                .Select(response => response.RootElement
                    .GetProperty("result")
                    .GetProperty("runId")
                    .GetInt64())
                .ToArray();
            Assert.Equal(2, runIds.Distinct().Count());
        }
        finally
        {
            foreach (JsonDocument response in responses)
            {
                response.Dispose();
            }
        }

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(2L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM stage_runs;"));
        Assert.Equal(1L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM stage_runs WHERE status = 0;"));
        Assert.Equal(1L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM stage_runs WHERE status = 2;"));
    }

    [Fact]
    public async Task DeletedUserCannotCreateStageRun()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "stage-deleted-user");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "DELETE FROM users WHERE id = @UserId;",
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "stage.enter",
            new { stageId = 1 },
            session.AccessToken);

        AuthRpcClient.HasError(document, 1101, "USER_NOT_FOUND");
        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM stage_runs;"));
    }
}
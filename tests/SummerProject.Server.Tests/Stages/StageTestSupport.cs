using System.Text.Json;

using SummerProject.Server.Tests.Auth;

namespace SummerProject.Server.Tests.Stages;

internal static class StageTestSupport
{
    public static async Task<long> EnterAsync(
        HttpClient client,
        string accessToken,
        long stageId = 1)
    {
        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "stage.enter",
            new { stageId },
            accessToken);
        return document.RootElement.GetProperty("result").GetProperty("runId").GetInt64();
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private readonly object _sync = new();
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_sync)
        {
            return _utcNow;
        }
    }

    public void Advance(TimeSpan amount)
    {
        lock (_sync)
        {
            _utcNow = _utcNow.Add(amount);
        }
    }
}
using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Rooms;

public sealed class RoomConcurrencyTests
{
    [Fact]
    public async Task ConcurrentUpsertsLeaveOneCompleteSnapshotForUser()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "concurrent-room");
        RoomTrapRequest[] first = [Trap(1, 1)];
        RoomTrapRequest[] second = [Trap(2, 2), Trap(3, 3)];

        JsonDocument[] responses = await Task.WhenAll(
            AuthRpcClient.PostAsync(
                client,
                "room.upsertMine",
                new { mapId = 1, traps = first },
                session.AccessToken),
            AuthRpcClient.PostAsync(
                client,
                "room.upsertMine",
                new { mapId = 1, traps = second },
                session.AccessToken));
        try
        {
            Assert.All(responses, response =>
                Assert.True(response.RootElement.TryGetProperty("result", out _)));
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
            "SELECT COUNT(*) FROM user_rooms WHERE user_id = @UserId;",
            new { session.UserId }));
        string storedJson = await connection.QuerySingleAsync<string>(
            "SELECT traps_json FROM user_rooms WHERE user_id = @UserId;",
            new { session.UserId });
        using JsonDocument stored = JsonDocument.Parse(storedJson);
        Assert.Contains(stored.RootElement.GetArrayLength(), new[] { 1, 2 });
    }

    private static RoomTrapRequest Trap(int x, int y) =>
        new(0, new RoomPositionRequest(x, y, 0), new RoomRotationRequest(0, 0, 0, 1));
}
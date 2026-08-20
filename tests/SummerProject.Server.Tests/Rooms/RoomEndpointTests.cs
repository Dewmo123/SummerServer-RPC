using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;
using SummerProject.Server.Tests.Rpc;

namespace SummerProject.Server.Tests.Rooms;

public sealed class RoomEndpointTests
{
    [Fact]
    public async Task UpsertAndGetReturnValidatedRoomSnapshot()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "room-owner");
        RoomTrapRequest[] traps =
        [
            Trap(0, 0, w: Math.Sqrt(0.98)),
            Trap(15, 7, w: Math.Sqrt(1.02))
        ];

        using JsonDocument upsert = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps },
            session.AccessToken);
        using JsonDocument get = await AuthRpcClient.PostAsync(
            client,
            "room.getMine",
            bearerToken: session.AccessToken);

        JsonElement upsertRoom = upsert.RootElement.GetProperty("result").GetProperty("room");
        JsonElement getRoom = get.RootElement.GetProperty("result").GetProperty("room");
        Assert.True(JsonElement.DeepEquals(upsertRoom, getRoom));
        Assert.Equal(session.UserId, getRoom.GetProperty("userId").GetInt64());
        Assert.Equal(1L, getRoom.GetProperty("map").GetProperty("mapId").GetInt64());
        Assert.Equal(16, getRoom.GetProperty("map").GetProperty("width").GetInt32());
        Assert.Equal(8, getRoom.GetProperty("map").GetProperty("height").GetInt32());
        Assert.Equal(2, getRoom.GetProperty("traps").GetArrayLength());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        dynamic stored = await connection.QuerySingleAsync(
            "SELECT map_id, traps_json FROM user_rooms WHERE user_id = @UserId;",
            new { session.UserId });
        Assert.Equal(1L, (long)stored.map_id);
        using JsonDocument snapshot = JsonDocument.Parse((string)stored.traps_json);
        Assert.Equal(2, snapshot.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task PositionalUpsertAndEmptyObjectGetFollowPublishedParamsContract()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "room-params-order");

        using JsonDocument upsert = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new object[] { 1, Array.Empty<object>() },
            session.AccessToken);
        using JsonDocument get = await AuthRpcClient.PostAsync(
            client,
            "room.getMine",
            new { },
            session.AccessToken);

        Assert.True(upsert.RootElement.TryGetProperty("result", out _));
        Assert.True(get.RootElement.TryGetProperty("result", out _));
    }

    [Theory]
    [InlineData("room.upsertMine")]
    [InlineData("room.getMine")]
    public async Task RoomMethodsRequireAuthentication(string method)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        object? parameters = method == "room.upsertMine"
            ? new { mapId = 1, traps = Array.Empty<object>() }
            : null;

        using JsonDocument document = await AuthRpcClient.PostAsync(client, method, parameters);

        AuthRpcClient.HasError(document, -32001, "AUTH_UNAUTHENTICATED");
    }

    [Fact]
    public async Task MissingRoomAndMapReturnStableErrors()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "missing-room");

        using JsonDocument missingRoom = await AuthRpcClient.PostAsync(
            client,
            "room.getMine",
            bearerToken: session.AccessToken);
        using JsonDocument missingMap = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 999, traps = Array.Empty<object>() },
            session.AccessToken);

        AuthRpcClient.HasError(missingRoom, 1502, "ROOM_NOT_FOUND");
        AuthRpcClient.HasError(missingMap, 1501, "MAP_NOT_FOUND");
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM user_rooms;"));
    }

    [Fact]
    public async Task DeletedUserCannotCreateRoom()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "deleted-room-user");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "DELETE FROM users WHERE id = @UserId;",
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = Array.Empty<object>() },
            session.AccessToken);

        AuthRpcClient.HasError(document, 1101, "USER_NOT_FOUND");
        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM user_rooms;"));
    }

    [Fact]
    public async Task UpsertReplacesWholeSnapshotAndInvalidRequestLeavesPreviousRoom()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "replace-room");
        using JsonDocument first = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = new[] { Trap(1, 1), Trap(2, 2) } },
            session.AccessToken);

        using JsonDocument invalid = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = new[] { Trap(-1, 0) } },
            session.AccessToken);
        AuthRpcClient.HasError(invalid, 1505, "TRAP_OUT_OF_BOUNDS");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            string unchanged = await connection.QuerySingleAsync<string>(
                "SELECT traps_json FROM user_rooms WHERE user_id = @UserId;",
                new { session.UserId });
            using JsonDocument snapshot = JsonDocument.Parse(unchanged);
            Assert.Equal(2, snapshot.RootElement.GetArrayLength());
        }

        using JsonDocument replaced = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = Array.Empty<object>() },
            session.AccessToken);
        Assert.Empty(replaced.RootElement
            .GetProperty("result")
            .GetProperty("room")
            .GetProperty("traps")
            .EnumerateArray());

        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        string current = await verification.QuerySingleAsync<string>(
            "SELECT traps_json FROM user_rooms WHERE user_id = @UserId;",
            new { session.UserId });
        Assert.Equal("[]", current);
        Assert.Equal(1L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM user_rooms;"));
    }

    [Fact]
    public async Task MissingStoredMapReturnsIntegrityErrorAndOperationsLog()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "invalid-map-room");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms)
                VALUES (@UserId, 999, '[]', 1);
                """,
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.getMine",
            bearerToken: session.AccessToken);

        AuthRpcClient.HasError(document, 1503, "ROOM_MAP_INVALID");
        JsonRpcTestLogEntry integrityLog = Assert.Single(application.Logs, log =>
            log.Level == Microsoft.Extensions.Logging.LogLevel.Error
            && log.Category.Contains("RoomLayoutService", StringComparison.Ordinal));
        Assert.Equal(session.UserId, integrityLog.Properties["UserId"]);
        Assert.Equal(999L, integrityLog.Properties["MapId"]);
    }

    [Fact]
    public async Task InvalidStoredTrapSchemaReturnsInternalError()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "invalid-snapshot-room");
        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            // DB의 JSON 배열 제약만 통과한 손상 스냅샷도 조회 시 엄격한 Packet 스키마로 다시 검증한다.
            await connection.ExecuteAsync(
                """
                INSERT INTO user_rooms (user_id, map_id, traps_json, updated_at_utc_ms)
                VALUES (@UserId, 1, '[{"type":0,"unknown":true}]', 1);
                """,
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.getMine",
            bearerToken: session.AccessToken);

        AuthRpcClient.HasError(document, -32603, "RPC_INTERNAL_ERROR");
    }

    [Fact]
    public async Task RequestBodyAcceptsExactly64KiBAndRejectsOneByteMore()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "room-body-size");
        const int limit = 65_536;
        string envelope =
            "{\"jsonrpc\":\"2.0\",\"method\":\"room.upsertMine\",\"params\":{\"mapId\":1,\"traps\":[]},\"id\":1}";

        string exact = envelope + new string(' ', limit - Encoding.UTF8.GetByteCount(envelope));
        using HttpResponseMessage accepted = await SendRawAsync(client, exact, session.AccessToken);
        using HttpResponseMessage rejected = await SendRawAsync(client, exact + " ", session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rejected.StatusCode);
    }

    private static RoomTrapRequest Trap(
        int x,
        int y,
        int z = 0,
        double w = 1,
        int type = 0) =>
        new(type, new RoomPositionRequest(x, y, z), new RoomRotationRequest(0, 0, 0, w));

    private static async Task<HttpResponseMessage> SendRawAsync(
        HttpClient client,
        string json,
        string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/rpc");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await client.SendAsync(request);
    }
}

public sealed record RoomTrapRequest(
    int Type,
    RoomPositionRequest Position,
    RoomRotationRequest Rotation);

public sealed record RoomPositionRequest(int X, int Y, int Z);

public sealed record RoomRotationRequest(double X, double Y, double Z, double W);
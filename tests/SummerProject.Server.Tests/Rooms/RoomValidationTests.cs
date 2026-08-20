using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Rooms;

public sealed class RoomValidationTests
{
    public static TheoryData<RoomTrapRequest, int, string> InvalidTraps => new()
    {
        { Trap(0, 0, type: 99), 1504, "TRAP_TYPE_UNSUPPORTED" },
        { Trap(-1, 0), 1505, "TRAP_OUT_OF_BOUNDS" },
        { Trap(16, 0), 1505, "TRAP_OUT_OF_BOUNDS" },
        { Trap(0, -1), 1505, "TRAP_OUT_OF_BOUNDS" },
        { Trap(0, 8), 1505, "TRAP_OUT_OF_BOUNDS" },
        { Trap(0, 0, z: 1), 1505, "TRAP_OUT_OF_BOUNDS" },
        { Trap(0, 0, w: Math.Sqrt(0.979)), 1507, "TRAP_ROTATION_INVALID" },
        { Trap(0, 0, w: Math.Sqrt(1.021)), 1507, "TRAP_ROTATION_INVALID" }
    };

    [Theory]
    [MemberData(nameof(InvalidTraps))]
    public async Task InvalidTrapReturnsSpecificContractError(
        RoomTrapRequest trap,
        int expectedCode,
        string expectedKey)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            $"invalid-trap-{expectedCode}-{trap.Position.X}-{trap.Position.Y}-{trap.Position.Z}-{trap.Rotation.W}");

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = new[] { trap } },
            session.AccessToken);

        AuthRpcClient.HasError(document, expectedCode, expectedKey);
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM user_rooms;"));
    }

    [Fact]
    public async Task DuplicatePositionIsRejected()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "duplicate-room-trap");

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = new[] { Trap(3, 4), Trap(3, 4, w: -1) } },
            session.AccessToken);

        AuthRpcClient.HasError(document, 1506, "TRAP_POSITION_DUPLICATED");
    }

    [Theory]
    [InlineData(0.98)]
    [InlineData(1.02)]
    public async Task RotationMagnitudeSquaredBoundaryIsAccepted(double magnitudeSquared)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            $"rotation-room-{magnitudeSquared}");

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = new[] { Trap(0, 0, w: Math.Sqrt(magnitudeSquared)) } },
            session.AccessToken);

        Assert.True(document.RootElement.TryGetProperty("result", out _));
    }

    [Fact]
    public async Task OneHundredTrapsAreAcceptedAndOneHundredOneAreInvalidParams()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "trap-count-room");
        RoomTrapRequest[] oneHundred = Enumerable.Range(0, 100)
            .Select(index => Trap(index % 16, index / 16))
            .ToArray();
        RoomTrapRequest[] oneHundredOne = Enumerable.Range(0, 101)
            .Select(index => Trap(index % 16, index / 16))
            .ToArray();

        using JsonDocument accepted = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = oneHundred },
            session.AccessToken);
        using JsonDocument rejected = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = oneHundredOne },
            session.AccessToken);

        Assert.Equal(100, accepted.RootElement
            .GetProperty("result")
            .GetProperty("room")
            .GetProperty("traps")
            .GetArrayLength());
        AuthRpcClient.HasError(rejected, -32602, "RPC_INVALID_PARAMS");
        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        string storedJson = await connection.QuerySingleAsync<string>(
            "SELECT traps_json FROM user_rooms WHERE user_id = @UserId;",
            new { session.UserId });
        using JsonDocument stored = JsonDocument.Parse(storedJson);
        Assert.Equal(100, stored.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task MissingNestedPacketAndNullTrapArrayAreInvalidParams()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(client, "null-room-fields");

        using JsonDocument missingPosition = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new
            {
                mapId = 1,
                traps = new[] { new { type = 0, rotation = new { x = 0, y = 0, z = 0, w = 1 } } }
            },
            session.AccessToken);
        using JsonDocument nullTraps = await AuthRpcClient.PostAsync(
            client,
            "room.upsertMine",
            new { mapId = 1, traps = (object?)null },
            session.AccessToken);

        AuthRpcClient.HasError(missingPosition, -32602, "RPC_INVALID_PARAMS");
        AuthRpcClient.HasError(nullTraps, -32602, "RPC_INVALID_PARAMS");
    }

    private static RoomTrapRequest Trap(
        int x,
        int y,
        int z = 0,
        double w = 1,
        int type = 0) =>
        new(type, new RoomPositionRequest(x, y, z), new RoomRotationRequest(0, 0, 0, w));
}
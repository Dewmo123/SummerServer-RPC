using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Exceptions.Currencies;
using SummerProject.Server.Exceptions.Rooms;
using SummerProject.Server.Exceptions.Stages;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Dispatching;

namespace SummerProject.Server.Tests.Rpc;

public sealed class GameplayErrorMappingTests
{
    public static TheoryData<Exception, int, string> Cases => new()
    {
        { new UserNotFoundException(), 1101, "USER_NOT_FOUND" },
        { new CharacterNotFoundException(), 1201, "CHARACTER_NOT_FOUND" },
        { new CharacterInvalidExperienceException(), 1202, "CHARACTER_INVALID_EXPERIENCE" },
        { new CurrencyInvalidTypeException(), 1301, "CURRENCY_INVALID_TYPE" },
        { new CurrencyInsufficientException(), 1302, "CURRENCY_INSUFFICIENT" },
        { new CurrencyInvalidAmountException(), 1303, "CURRENCY_INVALID_AMOUNT" },
        { new CurrencyOverflowException(), 1304, "CURRENCY_OVERFLOW" },
        { new StageNotFoundException(), 1401, "STAGE_NOT_FOUND" },
        { new StageRunNotFoundException(), 1402, "STAGE_RUN_NOT_FOUND" },
        { new StageRunForbiddenException(), 1403, "STAGE_RUN_FORBIDDEN" },
        { new StageRunAlreadyCompletedException(), 1404, "STAGE_RUN_ALREADY_COMPLETED" },
        { new StageClearTooEarlyException(), 1405, "STAGE_CLEAR_TOO_EARLY" },
        { new StageRewardFailedException(), 1406, "STAGE_REWARD_FAILED" },
        { new MapNotFoundException(), 1501, "MAP_NOT_FOUND" },
        { new RoomNotFoundException(), 1502, "ROOM_NOT_FOUND" },
        { new RoomMapInvalidException(), 1503, "ROOM_MAP_INVALID" },
        { new TrapTypeUnsupportedException(), 1504, "TRAP_TYPE_UNSUPPORTED" },
        { new TrapOutOfBoundsException(), 1505, "TRAP_OUT_OF_BOUNDS" },
        { new TrapPositionDuplicatedException(), 1506, "TRAP_POSITION_DUPLICATED" },
        { new TrapRotationInvalidException(), 1507, "TRAP_ROTATION_INVALID" },
        { new RoomInvalidParamsException(), -32602, "RPC_INVALID_PARAMS" }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void GameplayExceptionUsesStableContractError(
        Exception exception,
        int expectedCode,
        string expectedKey)
    {
        JsonRpcErrorPacket error = new JsonRpcExceptionMapper().Map(exception, "trace-id");

        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedKey, error.Data.Key);
        Assert.Equal("trace-id", error.Data.TraceId);
    }
}
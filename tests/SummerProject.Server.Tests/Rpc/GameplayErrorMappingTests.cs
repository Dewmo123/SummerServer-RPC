using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Exceptions.Currencies;
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
        { new CurrencyOverflowException(), 1304, "CURRENCY_OVERFLOW" }
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
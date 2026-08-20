using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Exceptions.Currencies;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Services.Currencies;
using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Currencies;

public sealed class CurrencyBalanceServiceTests
{
    [Fact]
    public async Task IncreaseAndDecreaseChangeBalanceExactly()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-balance-user");
        using IServiceScope scope = application.Services.CreateScope();
        CurrencyBalanceService service =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();

        CurrencyProto increased = await service.IncreaseAsync(
            session.UserId,
            CurrencyTypeProto.Gold,
            100,
            CancellationToken.None);
        CurrencyProto decreased = await service.DecreaseAsync(
            session.UserId,
            CurrencyTypeProto.Gold,
            40,
            CancellationToken.None);

        Assert.Equal(new CurrencyProto(CurrencyTypeProto.Gold, 100), increased);
        Assert.Equal(new CurrencyProto(CurrencyTypeProto.Gold, 60), decreased);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveChangeAmountIsRejectedWithoutCreatingRow(long amount)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            $"currency-invalid-amount-{amount}");
        using IServiceScope scope = application.Services.CreateScope();
        CurrencyBalanceService service =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();

        await Assert.ThrowsAsync<CurrencyInvalidAmountException>(() =>
            service.IncreaseAsync(
                session.UserId,
                CurrencyTypeProto.Gold,
                amount,
                CancellationToken.None).AsTask());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task InsufficientBalanceDoesNotChangeStoredAmount()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-insufficient-user");
        using IServiceScope scope = application.Services.CreateScope();
        CurrencyBalanceService service =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();
        _ = await service.IncreaseAsync(
            session.UserId,
            CurrencyTypeProto.Gem,
            30,
            CancellationToken.None);

        await Assert.ThrowsAsync<CurrencyInsufficientException>(() =>
            service.DecreaseAsync(
                session.UserId,
                CurrencyTypeProto.Gem,
                31,
                CancellationToken.None).AsTask());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(30L, await connection.QuerySingleAsync<long>(
            "SELECT amount FROM currencies WHERE user_id = @UserId AND type = @Type;",
            new { session.UserId, Type = CurrencyTypeProto.Gem }));
    }

    [Fact]
    public async Task OverflowDoesNotChangeStoredAmount()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-overflow-user");
        using IServiceScope scope = application.Services.CreateScope();
        CurrencyBalanceService service =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();
        _ = await service.IncreaseAsync(
            session.UserId,
            CurrencyTypeProto.StageTicket,
            long.MaxValue - 5,
            CancellationToken.None);

        await Assert.ThrowsAsync<CurrencyOverflowException>(() =>
            service.IncreaseAsync(
                session.UserId,
                CurrencyTypeProto.StageTicket,
                10,
                CancellationToken.None).AsTask());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(long.MaxValue - 5, await connection.QuerySingleAsync<long>(
            "SELECT amount FROM currencies WHERE user_id = @UserId AND type = @Type;",
            new { session.UserId, Type = CurrencyTypeProto.StageTicket }));
    }

    [Fact]
    public async Task ConcurrentDecreaseAllowsOneWinnerWithoutNegativeBalance()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-decrease-race-user");
        using (IServiceScope initialScope = application.Services.CreateScope())
        {
            CurrencyBalanceService initialService =
                initialScope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();
            _ = await initialService.IncreaseAsync(
                session.UserId,
                CurrencyTypeProto.EventToken,
                100,
                CancellationToken.None);
        }

        Task[] changes = Enumerable.Range(0, 2)
            .Select(async _ =>
            {
                using IServiceScope scope = application.Services.CreateScope();
                CurrencyBalanceService service =
                    scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();
                await service.DecreaseAsync(
                    session.UserId,
                    CurrencyTypeProto.EventToken,
                    80,
                    CancellationToken.None);
            })
            .ToArray();
        Exception exception = await Assert.ThrowsAsync<CurrencyInsufficientException>(async () =>
            await Task.WhenAll(changes));
        Assert.IsType<CurrencyInsufficientException>(exception);

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(20L, await connection.QuerySingleAsync<long>(
            "SELECT amount FROM currencies WHERE user_id = @UserId AND type = @Type;",
            new { session.UserId, Type = CurrencyTypeProto.EventToken }));
    }

    [Fact]
    public async Task UnsupportedCurrencyTypeIsRejectedBeforeDatabaseChange()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-invalid-service-type-user");
        using IServiceScope scope = application.Services.CreateScope();
        CurrencyBalanceService service =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();

        await Assert.ThrowsAsync<CurrencyInvalidTypeException>(() =>
            service.IncreaseAsync(
                session.UserId,
                (CurrencyTypeProto)999,
                1,
                CancellationToken.None).AsTask());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }
}
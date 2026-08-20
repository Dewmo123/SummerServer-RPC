using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Services.Characters;
using SummerProject.Server.Services.Currencies;
using SummerProject.Server.Tests.Auth;

namespace SummerProject.Server.Tests.Gameplay;

public sealed class RewardTransactionCompositionTests
{
    [Fact]
    public async Task CharacterAndCurrencyRewardsCanShareCallerTransaction()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "shared-reward-transaction-user");
        using IServiceScope scope = application.Services.CreateScope();
        SqliteConnectionFactory connectionFactory =
            scope.ServiceProvider.GetRequiredService<SqliteConnectionFactory>();
        CharacterProgressionService characterService =
            scope.ServiceProvider.GetRequiredService<CharacterProgressionService>();
        CurrencyBalanceService currencyService =
            scope.ServiceProvider.GetRequiredService<CurrencyBalanceService>();

        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(CancellationToken.None);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        _ = await characterService.AddExperienceInTransactionAsync(
            connection,
            transaction,
            session.UserId,
            10,
            CancellationToken.None);
        _ = await currencyService.IncreaseInTransactionAsync(
            connection,
            transaction,
            session.UserId,
            CurrencyTypeProto.Gold,
            100,
            CancellationToken.None);

        // 이후 스테이지 처리 실패를 가정해 두 보상이 같은 트랜잭션으로 함께 취소되는지 검증한다.
        await transaction.RollbackAsync(CancellationToken.None);

        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
        Assert.Equal(0L, await verification.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }
}
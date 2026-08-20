using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Currencies;

public sealed class CurrencyEndpointTests
{
    [Theory]
    [InlineData("currency.getMine", true)]
    [InlineData("currency.listMine", false)]
    public async Task CurrencyQueriesRequireAuthentication(string method, bool hasParameters)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            method,
            hasParameters ? new { type = 1 } : null);

        AuthRpcClient.HasError(document, -32001, "AUTH_UNAUTHENTICATED");
    }

    [Fact]
    public async Task GetAndListMineLazilyCreateSupportedCurrenciesInCodeOrder()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-list-user");

        using JsonDocument single = await AuthRpcClient.PostAsync(
            client,
            "currency.getMine",
            new { type = 2 },
            session.AccessToken);
        JsonElement currency = single.RootElement
            .GetProperty("result")
            .GetProperty("currency");
        Assert.Equal(2, currency.GetProperty("type").GetInt32());
        Assert.Equal(0L, currency.GetProperty("amount").GetInt64());

        using JsonDocument list = await AuthRpcClient.PostAsync(
            client,
            "currency.listMine",
            bearerToken: session.AccessToken);
        JsonElement.ArrayEnumerator currencies = list.RootElement
            .GetProperty("result")
            .GetProperty("currencies")
            .EnumerateArray();
        Assert.Equal([1, 2, 3, 4], currencies.Select(item => item.GetProperty("type").GetInt32()));
        Assert.All(currencies, item => Assert.Equal(0L, item.GetProperty("amount").GetInt64()));

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(4L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task ConcurrentFirstListKeepsExactlyOneRowPerCurrencyType()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-concurrent-user");

        Task<JsonDocument>[] requests = Enumerable.Range(0, 8)
            .Select(_ => AuthRpcClient.PostAsync(
                client,
                "currency.listMine",
                bearerToken: session.AccessToken))
            .ToArray();
        JsonDocument[] responses = await Task.WhenAll(requests);
        try
        {
            Assert.All(responses, response => Assert.Equal(
                4,
                response.RootElement
                    .GetProperty("result")
                    .GetProperty("currencies")
                    .GetArrayLength()));
        }
        finally
        {
            foreach (JsonDocument response in responses)
            {
                response.Dispose();
            }
        }

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(4L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
        Assert.Equal(4L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(DISTINCT type) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task InvalidCurrencyTypeReturnsContractErrorWithoutCreatingRow()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "currency-invalid-type-user");

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "currency.getMine",
            new { type = 999 },
            session.AccessToken);
        AuthRpcClient.HasError(document, 1301, "CURRENCY_INVALID_TYPE");

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM currencies WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Theory]
    [InlineData("currency.getMine", true)]
    [InlineData("currency.listMine", false)]
    public async Task DeletedUserReturnsUserNotFound(string method, bool hasParameters)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            $"currency-deleted-{method}");

        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "DELETE FROM users WHERE id = @UserId;",
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            method,
            hasParameters ? new { type = 1 } : null,
            session.AccessToken);
        AuthRpcClient.HasError(document, 1101, "USER_NOT_FOUND");
    }
}
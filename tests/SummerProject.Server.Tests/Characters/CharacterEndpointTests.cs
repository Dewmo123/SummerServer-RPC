using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Characters;

public sealed class CharacterEndpointTests
{
    [Fact]
    public async Task GetMineRequiresAuthenticationAndCreatesCharacterOnce()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using JsonDocument unauthenticated = await AuthRpcClient.PostAsync(
            client,
            "character.getMine");
        AuthRpcClient.HasError(unauthenticated, -32001, "AUTH_UNAUTHENTICATED");

        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-get-user");
        using JsonDocument first = await AuthRpcClient.PostAsync(
            client,
            "character.getMine",
            bearerToken: session.AccessToken);
        using JsonDocument second = await AuthRpcClient.PostAsync(
            client,
            "character.getMine",
            bearerToken: session.AccessToken);

        JsonElement character = first.RootElement
            .GetProperty("result")
            .GetProperty("character");
        Assert.Equal(1L, character.GetProperty("level").GetInt64());
        Assert.Equal(0L, character.GetProperty("exp").GetInt64());
        Assert.Equal(100L, character.GetProperty("expToNextLevel").GetInt64());
        Assert.Equal(character.ToString(), second.RootElement
            .GetProperty("result")
            .GetProperty("character")
            .ToString());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(1L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task ConcurrentFirstGetKeepsSingleCharacter()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-concurrent-user");

        Task<JsonDocument>[] requests = Enumerable.Range(0, 8)
            .Select(_ => AuthRpcClient.PostAsync(
                client,
                "character.getMine",
                bearerToken: session.AccessToken))
            .ToArray();
        JsonDocument[] responses = await Task.WhenAll(requests);
        try
        {
            Assert.All(responses, response => Assert.Equal(
                1L,
                response.RootElement
                    .GetProperty("result")
                    .GetProperty("character")
                    .GetProperty("level")
                    .GetInt64()));
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
            "SELECT COUNT(*) FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task DeletedUserReturnsUserNotFoundWithoutCreatingCharacter()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-deleted-user");

        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "DELETE FROM users WHERE id = @UserId;",
                new { session.UserId });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "character.getMine",
            bearerToken: session.AccessToken);
        AuthRpcClient.HasError(document, 1101, "USER_NOT_FOUND");

        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await verification.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM characters;"));
    }
}
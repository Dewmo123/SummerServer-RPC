using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;

namespace SummerProject.Server.Tests.Auth;

public sealed class DevelopmentLoginTests
{
    [Fact]
    public async Task DevelopmentLoginReturnsConfiguredUser()
    {
        await using AuthenticationTestApplicationFactory application = new(
            environment: "Development",
            developmentLoginEnabled: true,
            developmentUsername: "local-developer");
        using HttpClient client = application.CreateClient();
        await InsertDevelopmentUserAsync(application.DatabasePath, "local-developer");

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.development");
        JsonElement result = document.RootElement.GetProperty("result");

        Assert.Equal("local-developer", result.GetProperty("username").GetString());
        Assert.False(string.IsNullOrWhiteSpace(AuthRpcClient.ReadAccessToken(result)));
        Assert.False(string.IsNullOrWhiteSpace(AuthRpcClient.ReadRefreshToken(result)));
    }

    [Fact]
    public async Task MissingDevelopmentUserReturnsContractError()
    {
        await using AuthenticationTestApplicationFactory application = new(
            environment: "Development",
            developmentLoginEnabled: true,
            developmentUsername: "missing-developer");
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.development");

        AuthRpcClient.HasError(document, 1004, "AUTH_DEVELOPMENT_USER_NOT_FOUND");
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public async Task DevelopmentLoginIsNotRegisteredWithoutBothGuards(
        string environment,
        bool enabled)
    {
        await using AuthenticationTestApplicationFactory application = new(
            environment,
            enabled);
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.development");

        AuthRpcClient.HasError(document, -32601, "RPC_METHOD_NOT_FOUND");
    }

    private static async Task InsertDevelopmentUserAsync(string databasePath, string username)
    {
        await using SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms)
            VALUES (@Username, 999, @ProviderUserId, @CreatedAtUtcMs);
            """,
            new
            {
                Username = username,
                ProviderUserId = $"development:{username}",
                CreatedAtUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
    }
}
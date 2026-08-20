using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Helpers.Auth;

namespace SummerProject.Server.Tests.Auth;

public sealed class AuthenticationEndpointTests
{
    [Fact]
    public async Task GoogleLoginCreatesUserAndStoresOnlyRefreshTokenHash()
    {
        const string subject = "google-user-' OR 1=1 --";
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken = $"valid:{subject}" });
        JsonElement result = document.RootElement.GetProperty("result");
        long userId = result.GetProperty("userId").GetInt64();
        string username = result.GetProperty("username").GetString()!;
        string accessToken = AuthRpcClient.ReadAccessToken(result);
        string refreshToken = AuthRpcClient.ReadRefreshToken(result);

        Assert.True(userId > 0);
        Assert.StartsWith("g_", username, StringComparison.Ordinal);
        Assert.DoesNotContain(subject, username, StringComparison.Ordinal);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), jwt.Subject);
        Assert.Equal(username, jwt.Claims.Single(claim => claim.Type == "username").Value);
        Assert.Equal("1", jwt.Claims.Single(claim => claim.Type == "provider").Value);
        Assert.False(string.IsNullOrWhiteSpace(jwt.Id));
        DateTimeOffset responseAccessExpiration = result.GetProperty("tokens")
            .GetProperty("accessTokenExpiresAt")
            .GetDateTimeOffset();
        Assert.Equal(new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero), responseAccessExpiration);

        await using SqliteConnection connection = Open(application.DatabasePath);
        dynamic user = await connection.QuerySingleAsync(
            "SELECT username, provider, provider_user_id FROM users;");
        Assert.Equal(username, (string)user.username);
        Assert.Equal(1L, (long)user.provider);
        Assert.Equal(subject, (string)user.provider_user_id);

        dynamic storedToken = await connection.QuerySingleAsync(
            "SELECT typeof(token_hash) AS storage_type, length(token_hash) AS hash_length FROM refresh_tokens;");
        Assert.Equal("blob", (string)storedToken.storage_type);
        Assert.Equal(32L, (long)storedToken.hash_length);
        string databaseHex = await connection.QuerySingleAsync<string>("SELECT hex(token_hash) FROM refresh_tokens;");
        Assert.DoesNotContain(refreshToken, databaseHex, StringComparison.Ordinal);
        Assert.DoesNotContain(
            application.Logs,
            log => log.Message.Contains(accessToken, StringComparison.Ordinal)
                || log.Message.Contains(refreshToken, StringComparison.Ordinal)
                || log.Message.Contains($"valid:{subject}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GoogleLoginUsesNextDeterministicUsernameWhenUniqueNameCollides()
    {
        const string subject = "username-collision-user";
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GoogleUsernameFactory usernameFactory =
            application.Services.GetRequiredService<GoogleUsernameFactory>();
        string firstCandidate = usernameFactory.CreateCandidates(subject).First();

        await using (SqliteConnection connection = Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO users (username, provider, provider_user_id, created_at_utc_ms)
                VALUES (@Username, 999, @ProviderUserId, @CreatedAtUtcMs);
                """,
                new
                {
                    Username = firstCandidate,
                    ProviderUserId = "guest-name-owner",
                    CreatedAtUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
        }

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken = $"valid:{subject}" });
        string actualUsername = document.RootElement
            .GetProperty("result")
            .GetProperty("username")
            .GetString()!;

        Assert.NotEqual(firstCandidate, actualUsername);
        Assert.Equal(usernameFactory.CreateCandidates(subject).Skip(1).First(), actualUsername);
        await using SqliteConnection verification = Open(application.DatabasePath);
        Assert.Equal(2L, await verification.QuerySingleAsync<long>("SELECT COUNT(*) FROM users;"));
    }

    [Theory]
    [InlineData("invalid-signature")]
    [InlineData("invalid-audience")]
    [InlineData("valid:")]
    public async Task InvalidGoogleTokenReturnsContractError(string idToken)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken });

        AuthRpcClient.HasError(document, 1001, "AUTH_INVALID_GOOGLE_TOKEN");
        await using SqliteConnection connection = Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM users;"));
    }

    [Fact]
    public async Task ExistingAndConcurrentGoogleLoginKeepSingleUser()
    {
        const string idToken = "valid:concurrent-google-user";
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        Task<JsonDocument>[] requests = Enumerable.Range(0, 8)
            .Select(_ => AuthRpcClient.PostAsync(
                client,
                "auth.login.google",
                new { idToken }))
            .ToArray();
        JsonDocument[] responses = await Task.WhenAll(requests);

        try
        {
            long[] userIds = responses
                .Select(response => response.RootElement.GetProperty("result").GetProperty("userId").GetInt64())
                .ToArray();
            Assert.Single(userIds.Distinct());
            Assert.Equal(8, responses
                .Select(response => AuthRpcClient.ReadRefreshToken(response.RootElement.GetProperty("result")))
                .Distinct(StringComparer.Ordinal)
                .Count());
        }
        finally
        {
            foreach (JsonDocument response in responses)
            {
                response.Dispose();
            }
        }

        await using SqliteConnection connection = Open(application.DatabasePath);
        Assert.Equal(1L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM users;"));
        Assert.Equal(8L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM refresh_tokens;"));
    }

    [Fact]
    public async Task RefreshRotationPreservesAbsoluteExpirationAndReuseRevokesFamily()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        (string original, string originalExpiration) = await LoginAndReadRefreshAsync(client, "rotation-user");

        using JsonDocument rotatedDocument = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken = original });
        JsonElement rotated = rotatedDocument.RootElement.GetProperty("result");
        string replacement = AuthRpcClient.ReadRefreshToken(rotated);
        string replacementExpiration = rotated.GetProperty("tokens")
            .GetProperty("refreshTokenExpiresAt")
            .GetString()!;

        Assert.NotEqual(original, replacement);
        Assert.Equal(originalExpiration, replacementExpiration);

        using JsonDocument reused = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken = original });
        AuthRpcClient.HasError(reused, 1003, "AUTH_REFRESH_TOKEN_REUSED");

        using JsonDocument revokedReplacement = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken = replacement });
        AuthRpcClient.HasError(revokedReplacement, 1002, "AUTH_INVALID_REFRESH_TOKEN");

        await using SqliteConnection connection = Open(application.DatabasePath);
        Assert.Equal(2L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM refresh_tokens;"));
        Assert.Equal(2L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE revoked_at_utc_ms IS NOT NULL;"));
        Assert.Equal(1L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE used_at_utc_ms IS NOT NULL;"));
    }

    [Fact]
    public async Task ConcurrentRefreshAllowsOneWinnerAndRevokesFamilyOnReuse()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        (string original, _) = await LoginAndReadRefreshAsync(client, "refresh-race-user");

        Task<JsonDocument>[] requests =
        [
            AuthRpcClient.PostAsync(client, "auth.token.refresh", new { refreshToken = original }),
            AuthRpcClient.PostAsync(client, "auth.token.refresh", new { refreshToken = original })
        ];
        JsonDocument[] responses = await Task.WhenAll(requests);

        try
        {
            Assert.Single(responses, response => response.RootElement.TryGetProperty("result", out _));
            JsonDocument loser = Assert.Single(
                responses,
                response => response.RootElement.TryGetProperty("error", out _));
            AuthRpcClient.HasError(loser, 1003, "AUTH_REFRESH_TOKEN_REUSED");
        }
        finally
        {
            foreach (JsonDocument response in responses)
            {
                response.Dispose();
            }
        }

        await using SqliteConnection connection = Open(application.DatabasePath);
        Assert.Equal(2L, await connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM refresh_tokens;"));
        Assert.Equal(2L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE revoked_at_utc_ms IS NOT NULL;"));
    }

    [Fact]
    public async Task ExpiredMissingAndLoggedOutRefreshTokensFollowContract()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        (string refreshToken, _) = await LoginAndReadRefreshAsync(client, "logout-user");

        using JsonDocument firstLogout = await AuthRpcClient.PostAsync(
            client,
            "auth.logout",
            new { refreshToken });
        using JsonDocument secondLogout = await AuthRpcClient.PostAsync(
            client,
            "auth.logout",
            new { refreshToken });
        using JsonDocument missingLogout = await AuthRpcClient.PostAsync(
            client,
            "auth.logout",
            new { refreshToken = "missing-token" });
        Assert.True(firstLogout.RootElement.GetProperty("result").GetProperty("completed").GetBoolean());
        Assert.True(secondLogout.RootElement.GetProperty("result").GetProperty("completed").GetBoolean());
        Assert.True(missingLogout.RootElement.GetProperty("result").GetProperty("completed").GetBoolean());

        using JsonDocument revoked = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken });
        AuthRpcClient.HasError(revoked, 1002, "AUTH_INVALID_REFRESH_TOKEN");

        using JsonDocument missing = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken = "missing-token" });
        AuthRpcClient.HasError(missing, 1002, "AUTH_INVALID_REFRESH_TOKEN");

        (string expiredToken, _) = await LoginAndReadRefreshAsync(client, "expired-user");
        await using SqliteConnection connection = Open(application.DatabasePath);
        await connection.ExecuteAsync(
            "UPDATE refresh_tokens SET expires_at_utc_ms = 0 WHERE revoked_at_utc_ms IS NULL;");
        using JsonDocument expired = await AuthRpcClient.PostAsync(
            client,
            "auth.token.refresh",
            new { refreshToken = expiredToken });
        AuthRpcClient.HasError(expired, 1002, "AUTH_INVALID_REFRESH_TOKEN");
    }

    private static async Task<(string Token, string ExpiresAt)> LoginAndReadRefreshAsync(
        HttpClient client,
        string subject)
    {
        using JsonDocument login = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken = $"valid:{subject}" });
        JsonElement tokens = login.RootElement.GetProperty("result").GetProperty("tokens");
        return (
            tokens.GetProperty("refreshToken").GetString()!,
            tokens.GetProperty("refreshTokenExpiresAt").GetString()!);
    }

    private static SqliteConnection Open(string databasePath)
    {
        SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        return connection;
    }
}
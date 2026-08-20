using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.IdentityModel.Tokens;

namespace SummerProject.Server.Tests.Auth;

public sealed class JwtAuthenticationTests
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private static readonly string SigningKey = new('t', 32);

    [Fact]
    public async Task IssuedJwtAuthenticatesProtectedMethodAndNormalizesCaller()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        using JsonDocument login = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken = "valid:jwt-user" });
        JsonElement loginResult = login.RootElement.GetProperty("result");
        string accessToken = AuthRpcClient.ReadAccessToken(loginResult);

        using JsonDocument protectedCall = await AuthRpcClient.PostAsync(
            client,
            "test.protected",
            bearerToken: accessToken);
        JsonElement protectedResult = protectedCall.RootElement.GetProperty("result");

        Assert.Equal(loginResult.GetProperty("userId").GetInt64(), protectedResult.GetProperty("userId").GetInt64());
        Assert.Equal(loginResult.GetProperty("username").GetString(), protectedResult.GetProperty("username").GetString());
        Assert.Contains(
            application.Logs,
            log => log.Properties.TryGetValue("userId", out object? value)
                && Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)
                    == loginResult.GetProperty("userId").GetInt64());
        Assert.DoesNotContain(
            application.Logs,
            log => log.Message.Contains(accessToken, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingOrInvalidJwtReturnsUnauthenticatedError()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        string nowValid = CreateToken(Issuer, Audience, SigningKey, DateTime.UtcNow.AddMinutes(5));
        string changedSignature = nowValid[..^1] + (nowValid[^1] == 'a' ? 'b' : 'a');

        string[] invalidTokens =
        [
            changedSignature,
            CreateToken("wrong-issuer", Audience, SigningKey, DateTime.UtcNow.AddMinutes(5)),
            CreateToken(Issuer, "wrong-audience", SigningKey, DateTime.UtcNow.AddMinutes(5)),
            CreateToken(Issuer, Audience, SigningKey, DateTime.UtcNow.AddMinutes(-2)),
            CreateToken(Issuer, Audience, new string('x', 32), DateTime.UtcNow.AddMinutes(5)),
            CreateToken(Issuer, Audience, SigningKey, DateTime.UtcNow.AddMinutes(5), includeSubject: false)
        ];

        using JsonDocument missing = await AuthRpcClient.PostAsync(client, "test.protected");
        AuthRpcClient.HasError(missing, -32001, "AUTH_UNAUTHENTICATED");

        foreach (string token in invalidTokens)
        {
            using JsonDocument invalid = await AuthRpcClient.PostAsync(
                client,
                "test.protected",
                bearerToken: token);
            AuthRpcClient.HasError(invalid, -32001, "AUTH_UNAUTHENTICATED");
        }
    }

    private static string CreateToken(
        string issuer,
        string audience,
        string signingKey,
        DateTime expires,
        bool includeSubject = true)
    {
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new("username", "jwt-test-user"),
            new("provider", "1")
        ];
        if (includeSubject)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, "42"));
        }

        JwtSecurityToken jwt = new(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Tests.Auth;

public sealed class GoogleIdTokenValidatorTests
{
    private const string ClientId = "allowed-client-id";

    [Fact]
    public async Task MalformedTokenIsRejectedBeforeMetadataRequest()
    {
        GoogleIdTokenValidator validator = new(
            Options.Create(new GoogleAuthOptions
            {
                ClientIds = ["allowed-client-id"]
            }));

        await Assert.ThrowsAsync<InvalidGoogleTokenException>(async () =>
            await validator.ValidateAsync("not-a-jwt", CancellationToken.None));
    }

    [Fact]
    public async Task ValidGoogleTokenReturnsSubject()
    {
        using RSA rsa = RSA.Create(2048);
        GoogleIdTokenValidator validator = CreateValidator(rsa);
        string token = CreateToken(rsa, ClientId, "google-subject");

        GoogleIdentityProto identity = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.Equal("google-subject", identity.Subject);
    }

    [Theory]
    [InlineData("wrong-client-id", "google-subject", "https://accounts.google.com")]
    [InlineData("allowed-client-id", null, "https://accounts.google.com")]
    [InlineData("allowed-client-id", "google-subject", "https://wrong-issuer.example")]
    public async Task AudienceSubjectAndIssuerViolationsAreRejected(
        string audience,
        string? subject,
        string issuer)
    {
        using RSA rsa = RSA.Create(2048);
        GoogleIdTokenValidator validator = CreateValidator(rsa);
        string token = CreateToken(rsa, audience, subject, issuer);

        await Assert.ThrowsAsync<InvalidGoogleTokenException>(async () =>
            await validator.ValidateAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task UnknownSigningKeyIsRejectedAfterMetadataRefresh()
    {
        using RSA trustedRsa = RSA.Create(2048);
        using RSA untrustedRsa = RSA.Create(2048);
        StaticConfigurationManager configurationManager = CreateConfigurationManager(trustedRsa);
        GoogleIdTokenValidator validator = new(
            Options.Create(new GoogleAuthOptions { ClientIds = [ClientId] }),
            configurationManager);
        string token = CreateToken(
            untrustedRsa,
            ClientId,
            "google-subject",
            keyId: "unknown-key");

        await Assert.ThrowsAsync<InvalidGoogleTokenException>(async () =>
            await validator.ValidateAsync(token, CancellationToken.None));
        Assert.Equal(1, configurationManager.RefreshRequestCount);
    }

    private static GoogleIdTokenValidator CreateValidator(RSA rsa) =>
        new(
            Options.Create(new GoogleAuthOptions { ClientIds = [ClientId] }),
            CreateConfigurationManager(rsa));

    private static StaticConfigurationManager CreateConfigurationManager(RSA rsa)
    {
        RsaSecurityKey key = new(rsa) { KeyId = "test-key" };
        OpenIdConnectConfiguration configuration = new();
        configuration.SigningKeys.Add(key);
        return new StaticConfigurationManager(configuration);
    }

    private static string CreateToken(
        RSA rsa,
        string audience,
        string? subject,
        string issuer = "https://accounts.google.com",
        string keyId = "test-key")
    {
        List<Claim> claims = [];
        if (subject is not null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        }

        RsaSecurityKey key = new(rsa) { KeyId = keyId };
        JwtSecurityToken token = new(
            issuer,
            audience,
            claims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class StaticConfigurationManager(OpenIdConnectConfiguration configuration)
        : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public int RefreshRequestCount { get; private set; }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) =>
            Task.FromResult(configuration);

        public void RequestRefresh() => RefreshRequestCount++;
    }
}
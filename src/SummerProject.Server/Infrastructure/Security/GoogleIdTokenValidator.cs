using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Infrastructure.Security;

internal interface IGoogleIdTokenValidator
{
    ValueTask<GoogleIdentityProto> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken);
}

internal sealed class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private const string DiscoveryEndpoint =
        "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] ValidIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly GoogleAuthOptions _options;

    public GoogleIdTokenValidator(IOptions<GoogleAuthOptions> options)
        : this(
            options,
            new ConfigurationManager<OpenIdConnectConfiguration>(
                DiscoveryEndpoint,
                new OpenIdConnectConfigurationRetriever()))
    {
    }

    internal GoogleIdTokenValidator(
        IOptions<GoogleAuthOptions> options,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager)
    {
        _options = options.Value;
        _configurationManager = configurationManager;
    }

    public async ValueTask<GoogleIdentityProto> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        JwtSecurityTokenHandler tokenHandler = new()
        {
            MapInboundClaims = false
        };
        if (string.IsNullOrWhiteSpace(idToken) || !tokenHandler.CanReadToken(idToken))
        {
            throw new InvalidGoogleTokenException();
        }

        OpenIdConnectConfiguration configuration =
            await _configurationManager.GetConfigurationAsync(cancellationToken);
        TokenValidationResult validationResult = await tokenHandler.ValidateTokenAsync(
            idToken,
            CreateValidationParameters(configuration));

        if (!validationResult.IsValid
            && validationResult.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Google 키 회전 직후의 서명 실패는 메타데이터를 한 번 새로 받은 뒤 다시 판정한다.
            _configurationManager.RequestRefresh();
            configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            validationResult = await tokenHandler.ValidateTokenAsync(
                idToken,
                CreateValidationParameters(configuration));
        }

        if (!validationResult.IsValid)
        {
            throw new InvalidGoogleTokenException(validationResult.Exception);
        }

        string? subject = validationResult.ClaimsIdentity?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
        {
            throw new InvalidGoogleTokenException();
        }

        return new GoogleIdentityProto(subject);
    }

    private TokenValidationParameters CreateValidationParameters(
        OpenIdConnectConfiguration configuration) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers,
            ValidateAudience = true,
            ValidAudiences = _options.ClientIds,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
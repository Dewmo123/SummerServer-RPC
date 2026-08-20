using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.Datas.Auth;

namespace SummerProject.Server.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public IssuedAccessTokenProto Issue(UserModel user, DateTimeOffset now) =>
        Issue(user.Id, user.Username, user.Provider, now);

    public IssuedAccessTokenProto Issue(
        long userId,
        string username,
        LoginProviderProto provider,
        DateTimeOffset now)
    {
        JwtOptions settings = options.Value;
        DateTimeOffset issuedAt = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds());
        DateTimeOffset expiresAt = issuedAt.AddMinutes(settings.AccessTokenMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new("username", username),
            new("provider", ((int)provider).ToString(CultureInfo.InvariantCulture))
        ];

        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            settings.Issuer,
            settings.Audience,
            claims,
            issuedAt.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new IssuedAccessTokenProto(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    internal static TokenValidationParameters CreateValidationParameters(JwtOptions options) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
            NameClaimType = "username"
        };
}
using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Models.DTOs.Auth;

public sealed record TokenPairPacket(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    internal static TokenPairPacket From(IssuedTokenPairProto tokens) =>
        new(
            tokens.AccessToken.Value,
            tokens.AccessToken.ExpiresAt,
            tokens.RefreshToken.Value,
            tokens.RefreshToken.ExpiresAt);
}

public sealed record GoogleLoginResponse(
    long UserId,
    string Username,
    TokenPairPacket Tokens);

public sealed record DevelopmentLoginResponse(
    long UserId,
    string Username,
    TokenPairPacket Tokens);

public sealed record RefreshTokenResponse(TokenPairPacket Tokens);

public sealed record LogoutResponse(bool Completed);
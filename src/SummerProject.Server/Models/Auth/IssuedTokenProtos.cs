namespace SummerProject.Server.Models.Auth;

internal sealed record IssuedAccessTokenProto(
    string Value,
    DateTimeOffset ExpiresAt);

internal sealed record IssuedRefreshTokenProto(
    string Id,
    string FamilyId,
    string Value,
    byte[] Hash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

internal sealed record IssuedTokenPairProto(
    IssuedAccessTokenProto AccessToken,
    IssuedRefreshTokenProto RefreshToken);

internal sealed record LoginSessionProto(
    long UserId,
    string Username,
    IssuedTokenPairProto Tokens);
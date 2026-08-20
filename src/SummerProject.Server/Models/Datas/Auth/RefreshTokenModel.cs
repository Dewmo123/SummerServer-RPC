using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Models.Datas.Auth;

internal sealed class RefreshTokenModel
{
    public required string Id { get; init; }

    public long UserId { get; init; }

    public required string FamilyId { get; init; }

    public required byte[] TokenHash { get; init; }

    public long CreatedAtUtcMs { get; init; }

    public long ExpiresAtUtcMs { get; init; }

    public long? UsedAtUtcMs { get; init; }

    public long? RevokedAtUtcMs { get; init; }

    public string? RevokeReason { get; init; }

    public string? ReplacedByTokenId { get; init; }

    public required string Username { get; init; }

    public LoginProviderProto Provider { get; init; }
}
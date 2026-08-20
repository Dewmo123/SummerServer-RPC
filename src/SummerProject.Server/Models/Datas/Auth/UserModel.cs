using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Models.Datas.Auth;

internal sealed class UserModel
{
    public long Id { get; init; }

    public required string Username { get; init; }

    public LoginProviderProto Provider { get; init; }

    public required string ProviderUserId { get; init; }

    public long CreatedAtUtcMs { get; init; }
}
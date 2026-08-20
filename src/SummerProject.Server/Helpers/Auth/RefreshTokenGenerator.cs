using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;

namespace SummerProject.Server.Helpers.Auth;

internal sealed class RefreshTokenGenerator(IOptions<RefreshTokenOptions> options)
{
    private const int TokenByteLength = 32;

    public IssuedRefreshTokenProto CreateNew(DateTimeOffset now)
    {
        DateTimeOffset createdAt = TruncateToMilliseconds(now);
        string familyId = NewIdentifier();
        return Create(familyId, createdAt, createdAt.AddDays(options.Value.LifetimeDays));
    }

    public IssuedRefreshTokenProto CreateReplacement(
        string familyId,
        DateTimeOffset now,
        DateTimeOffset absoluteExpiration) =>
        Create(familyId, TruncateToMilliseconds(now), absoluteExpiration);

    public byte[] Hash(string rawToken)
    {
        // DB에는 원문을 복구할 수 없는 고정 길이 해시만 저장한다.
        return SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
    }

    private static IssuedRefreshTokenProto Create(
        string familyId,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        string value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new IssuedRefreshTokenProto(
            NewIdentifier(),
            familyId,
            value,
            hash,
            now,
            expiresAt);
    }

    private static string NewIdentifier() => Guid.NewGuid().ToString("D");

    private static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value) =>
        DateTimeOffset.FromUnixTimeMilliseconds(value.ToUnixTimeMilliseconds());
}
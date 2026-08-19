namespace SummerProject.Server.Infrastructure.Logging;

/// <summary>
/// 구조화 로그 필드에서 보안상 기록할 수 없는 이름과 과도하게 긴 값을 제거합니다.
/// </summary>
public sealed class SensitiveLogFilter
{
    private const int MaximumValueLength = 128;

    private static readonly string[] SensitiveNameFragments =
    [
        "authorization",
        "token",
        "signingkey",
        "params",
        "requestbody",
        "provideruserid"
    ];

    /// <summary>
    /// 필드명이 민감정보 차단 정책을 통과하면 제어 문자와 길이를 정규화한 값을 반환합니다.
    /// </summary>
    public bool TryFilter(string propertyName, string? value, out string? safeValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (SensitiveNameFragments.Any(
            fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            safeValue = null;
            return false;
        }

        if (value is null)
        {
            safeValue = null;
            return true;
        }

        string normalized = value.Replace('\r', ' ').Replace('\n', ' ');
        safeValue = normalized.Length <= MaximumValueLength
            ? normalized
            : normalized[..MaximumValueLength];
        return true;
    }
}
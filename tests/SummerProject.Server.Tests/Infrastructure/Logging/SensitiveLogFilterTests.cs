using SummerProject.Server.Infrastructure.Logging;

namespace SummerProject.Server.Tests.Infrastructure.Logging;

public sealed class SensitiveLogFilterTests
{
    // 이름만으로도 민감정보 가능성이 있는 필드는 값과 관계없이 거부해야 한다.
    [Theory]
    [InlineData("Authorization")]
    [InlineData("accessToken")]
    [InlineData("refreshToken")]
    [InlineData("googleIdToken")]
    [InlineData("signingKey")]
    [InlineData("params")]
    [InlineData("requestBody")]
    [InlineData("providerUserId")]
    public void SensitivePropertyNamesAreRejected(string propertyName)
    {
        SensitiveLogFilter filter = new();

        bool accepted = filter.TryFilter(propertyName, "must-not-log", out string? safeValue);

        Assert.False(accepted);
        Assert.Null(safeValue);
    }

    [Fact]
    public void SafeValuesAreLengthLimitedAndControlCharactersAreRemoved()
    {
        SensitiveLogFilter filter = new();
        string value = new string('a', 140) + "\r\nlog-forging";

        bool accepted = filter.TryFilter("rpcMethod", value, out string? safeValue);

        Assert.True(accepted);
        Assert.NotNull(safeValue);
        Assert.Equal(128, safeValue.Length);
        Assert.DoesNotContain('\r', safeValue);
        Assert.DoesNotContain('\n', safeValue);
    }
}
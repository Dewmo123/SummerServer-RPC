namespace SummerProject.Server.Infrastructure.Security;

// 리프레시 토큰 수명 정책은 액세스 토큰 설정과 분리해 검증한다.
internal sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int LifetimeDays { get; set; } = 30;
}
namespace SummerProject.Server.Infrastructure.Security;

// JWT 발급과 검증에 공통으로 사용할 보안 설정을 한 계약으로 관리한다.
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    // 서명 키는 appsettings에 저장하지 않고 환경 변수나 비밀 저장소에서 주입한다.
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int ClockSkewSeconds { get; set; } = 30;
}
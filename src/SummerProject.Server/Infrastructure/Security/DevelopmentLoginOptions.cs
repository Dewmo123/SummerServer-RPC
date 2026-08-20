namespace SummerProject.Server.Infrastructure.Security;

// 개발 로그인은 환경 이름과 별도로 명시적으로 활성화해야 한다.
internal sealed class DevelopmentLoginOptions
{
    public const string SectionName = "DevelopmentLogin";

    public bool Enabled { get; set; }

    public string Username { get; set; } = "developer";
}
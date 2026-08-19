namespace SummerProject.Server.Infrastructure.Security;

// 허용할 Google 애플리케이션 식별자는 외부 구성에서만 주입한다.
internal sealed class GoogleAuthOptions
{
    public const string SectionName = "Google";

    public string[] ClientIds { get; set; } = [];
}
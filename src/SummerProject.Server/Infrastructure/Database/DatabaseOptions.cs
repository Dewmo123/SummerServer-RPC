namespace SummerProject.Server.Infrastructure.Database;

// SQLite 연결마다 동일하게 적용할 경로와 잠금 정책을 정의한다.
internal sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Path { get; set; } = string.Empty;

    public int BusyTimeoutMilliseconds { get; set; } = 5_000;

    public bool UseWriteAheadLogging { get; set; } = true;
}
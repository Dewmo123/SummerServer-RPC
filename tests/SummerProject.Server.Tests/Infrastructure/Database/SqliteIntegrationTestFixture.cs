using Microsoft.Data.Sqlite;

namespace SummerProject.Server.Tests.Infrastructure.Database;

internal sealed class SqliteIntegrationTestFixture : IDisposable
{
    private static readonly string TestRootPath = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "SummerProject.Server.Tests"));

    private int _disposed;

    public SqliteIntegrationTestFixture()
    {
        DirectoryPath = Path.Combine(TestRootPath, Guid.NewGuid().ToString("N"));
        DatabasePath = Path.Combine(DirectoryPath, "integration.db");
    }

    public string DirectoryPath { get; }

    public string DatabasePath { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        if (!Directory.Exists(DirectoryPath))
        {
            return;
        }

        string resolvedPath = Path.GetFullPath(DirectoryPath);
        string requiredPrefix = TestRootPath.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!resolvedPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SQLite 테스트 디렉터리가 허용된 임시 경로를 벗어났습니다.");
        }

        // WAL과 SHM까지 테스트별 전용 디렉터리와 함께 제거해 다음 테스트와 격리한다.
        Directory.Delete(resolvedPath, recursive: true);
    }
}
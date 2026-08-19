using System.Globalization;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace SummerProject.Server.Infrastructure.Database;

internal sealed class SqliteConnectionFactory
{
    private readonly int _busyTimeoutMilliseconds;
    private readonly string _connectionString;

    public SqliteConnectionFactory(
        IOptions<DatabaseOptions> options,
        IWebHostEnvironment environment)
    {
        DatabaseOptions databaseOptions = options.Value;
        DatabasePath = Path.GetFullPath(
            Path.IsPathFullyQualified(databaseOptions.Path)
                ? databaseOptions.Path
                : Path.Combine(environment.ContentRootPath, databaseOptions.Path));
        _busyTimeoutMilliseconds = databaseOptions.BusyTimeoutMilliseconds;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    internal string DatabasePath { get; }

    internal void EnsureDatabaseDirectory()
    {
        string? directoryPath = Path.GetDirectoryName(DatabasePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException("SQLite DB 디렉터리를 확인할 수 없습니다.");
        }

        Directory.CreateDirectory(directoryPath);
    }

    public async ValueTask<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        SqliteConnection connection = new(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                "PRAGMA foreign_keys = ON;",
                cancellationToken: cancellationToken));

            // SQLite PRAGMA는 값 매개변수를 지원하지 않으므로 검증된 정수만 문화권과 무관하게 삽입한다.
            string busyTimeoutSql = string.Format(
                CultureInfo.InvariantCulture,
                "PRAGMA busy_timeout = {0};",
                _busyTimeoutMilliseconds);
            await connection.ExecuteAsync(new CommandDefinition(
                busyTimeoutSql,
                cancellationToken: cancellationToken));
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
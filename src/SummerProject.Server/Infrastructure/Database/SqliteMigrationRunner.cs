using System.Data.Common;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace SummerProject.Server.Infrastructure.Database;

internal sealed class SqliteMigrationRunner(
    SqliteConnectionFactory connectionFactory,
    EmbeddedSqlMigrationSource migrationSource,
    IOptions<DatabaseOptions> options,
    TimeProvider timeProvider,
    ILogger<SqliteMigrationRunner> logger)
{
    private const string CreateMigrationTableSql = """
        CREATE TABLE IF NOT EXISTS schema_migrations (
            version INTEGER PRIMARY KEY
                CONSTRAINT ck_schema_migrations_version CHECK (version > 0),
            name TEXT NOT NULL
                CONSTRAINT ck_schema_migrations_name CHECK (length(name) > 0),
            checksum TEXT NOT NULL
                CONSTRAINT ck_schema_migrations_checksum CHECK (length(checksum) = 64),
            applied_at_utc_ms INTEGER NOT NULL
        );
        """;

    private const string SelectAppliedMigrationsSql = """
        SELECT version AS Version,
               name AS Name,
               checksum AS Checksum,
               applied_at_utc_ms AS AppliedAtUtcMs
        FROM schema_migrations
        ORDER BY version;
        """;

    private const string InsertAppliedMigrationSql = """
        INSERT INTO schema_migrations (version, name, checksum, applied_at_utc_ms)
        VALUES (@Version, @Name, @Checksum, @AppliedAtUtcMs);
        """;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        connectionFactory.EnsureDatabaseDirectory();
        await using SqliteConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await ConfigureJournalModeAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            CreateMigrationTableSql,
            cancellationToken: cancellationToken));

        IReadOnlyList<SqlMigrationProto> migrations = await migrationSource.LoadAsync(cancellationToken);
        IReadOnlyDictionary<long, SchemaMigrationModel> applied =
            await ReadAppliedMigrationsAsync(connection, cancellationToken);
        ValidateAppliedMigrations(migrations, applied, requireAll: false);

        int appliedCount = 0;
        foreach (SqlMigrationProto migration in migrations)
        {
            if (applied.ContainsKey(migration.Version))
            {
                continue;
            }

            await ApplyAsync(connection, migration, cancellationToken);
            appliedCount++;
            logger.LogInformation(
                "SQLite 마이그레이션을 적용했습니다. migration: {migration}, version: {version}",
                migration.Name,
                migration.Version);
        }

        await VerifyAsync(connection, cancellationToken);
        logger.LogInformation(
            "SQLite 초기화를 완료했습니다. appliedCount: {appliedCount}",
            appliedCount);
    }

    internal async Task VerifyAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SqlMigrationProto> migrations = await migrationSource.LoadAsync(cancellationToken);
        IReadOnlyDictionary<long, SchemaMigrationModel> applied =
            await ReadAppliedMigrationsAsync(connection, cancellationToken);
        ValidateAppliedMigrations(migrations, applied, requireAll: true);
    }

    private async Task ConfigureJournalModeAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        if (!options.Value.UseWriteAheadLogging)
        {
            return;
        }

        string journalMode = await connection.QuerySingleAsync<string>(new CommandDefinition(
            "PRAGMA journal_mode = WAL;",
            cancellationToken: cancellationToken));
        if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new SqliteMigrationException("SQLite WAL 모드를 활성화할 수 없습니다.");
        }
    }

    private static async Task<IReadOnlyDictionary<long, SchemaMigrationModel>> ReadAppliedMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        IEnumerable<SchemaMigrationModel> rows = await connection.QueryAsync<SchemaMigrationModel>(
            new CommandDefinition(
                SelectAppliedMigrationsSql,
                cancellationToken: cancellationToken));
        return rows.ToDictionary(row => row.Version);
    }

    private static void ValidateAppliedMigrations(
        IReadOnlyList<SqlMigrationProto> migrations,
        IReadOnlyDictionary<long, SchemaMigrationModel> applied,
        bool requireAll)
    {
        IReadOnlyDictionary<int, SqlMigrationProto> available =
            migrations.ToDictionary(migration => migration.Version);

        foreach (SchemaMigrationModel row in applied.Values)
        {
            if (!available.TryGetValue(checked((int)row.Version), out SqlMigrationProto? migration))
            {
                throw new SqliteMigrationException(
                    $"적용된 DB 마이그레이션 파일을 찾을 수 없습니다: {row.Version:D4}");
            }

            if (!string.Equals(row.Name, migration.Name, StringComparison.Ordinal)
                || !string.Equals(row.Checksum, migration.Checksum, StringComparison.Ordinal))
            {
                throw new SqliteMigrationException(
                    $"DB 마이그레이션 이름 또는 체크섬이 현재 파일과 일치하지 않습니다: {row.Version:D4}");
            }
        }

        bool foundPendingMigration = false;
        foreach (SqlMigrationProto migration in migrations)
        {
            if (!applied.ContainsKey(migration.Version))
            {
                foundPendingMigration = true;
                continue;
            }

            if (foundPendingMigration)
            {
                throw new SqliteMigrationException(
                    $"DB 마이그레이션 적용 순서가 유효하지 않습니다: {migration.Name}");
            }
        }

        if (requireAll)
        {
            SqlMigrationProto? pending = migrations.FirstOrDefault(
                migration => !applied.ContainsKey(migration.Version));
            if (pending is not null)
            {
                throw new SqliteMigrationException(
                    $"적용되지 않은 DB 마이그레이션이 있습니다: {pending.Name}");
            }
        }
    }

    private async Task ApplyAsync(
        SqliteConnection connection,
        SqlMigrationProto migration,
        CancellationToken cancellationToken)
    {
        await using DbTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                migration.Sql,
                transaction: transaction,
                cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                InsertAppliedMigrationSql,
                new
                {
                    migration.Version,
                    migration.Name,
                    migration.Checksum,
                    AppliedAtUtcMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds()
                },
                transaction,
                cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RollbackWithoutMaskingAsync(transaction);
            throw new SqliteMigrationException(
                $"DB 마이그레이션 적용에 실패했습니다: {migration.Name}",
                exception);
        }
    }

    private static async Task RollbackWithoutMaskingAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // 원래 마이그레이션 오류가 롤백 오류에 가려지지 않도록 유지한다.
        }
    }
}
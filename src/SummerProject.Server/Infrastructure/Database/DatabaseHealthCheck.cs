using Dapper;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SummerProject.Server.Infrastructure.Database;

internal sealed class DatabaseHealthCheck(
    SqliteConnectionFactory connectionFactory,
    SqliteMigrationRunner migrationRunner) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using Microsoft.Data.Sqlite.SqliteConnection connection =
                await connectionFactory.OpenConnectionAsync(cancellationToken);
            int result = await connection.QuerySingleAsync<int>(new CommandDefinition(
                "SELECT 1;",
                cancellationToken: cancellationToken));
            if (result != 1)
            {
                return HealthCheckResult.Unhealthy("SQLite 상태 확인에 실패했습니다.");
            }

            await migrationRunner.VerifyAsync(connection, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // 상태 응답에는 DB 경로와 예외 세부 정보를 포함하지 않는다.
            return HealthCheckResult.Unhealthy("SQLite 상태 확인에 실패했습니다.");
        }
    }
}
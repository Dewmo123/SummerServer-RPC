using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using SummerProject.Server.Tests.Infrastructure.Database;

namespace SummerProject.Server.Tests.Infrastructure.Configuration;

internal static class TestServerSettings
{
    // 실제 비밀값 없이 시작 검증을 통과하도록 테스트 전용 설정만 메모리에 주입한다.
    public static IReadOnlyDictionary<string, string?> Valid { get; } =
        new Dictionary<string, string?>
        {
            ["JsonRpc:Path"] = "/rpc",
            ["JsonRpc:MaxRequestBodyBytes"] = "65536",
            ["JsonRpc:MaxBatchSize"] = "50",
            ["JsonRpc:MaxJsonDepth"] = "32",
            ["Database:Path"] = "test-data/test.db",
            ["Database:BusyTimeoutMilliseconds"] = "5000",
            ["Database:UseWriteAheadLogging"] = "true",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:SigningKey"] = new string('t', 32),
            ["Jwt:AccessTokenMinutes"] = "60",
            ["Jwt:ClockSkewSeconds"] = "30",
            ["RefreshToken:LifetimeDays"] = "30"
        };
}

public class ConfiguredServerApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteIntegrationTestFixture? _ownedDatabase;
    private int _databaseDisposed;

    public ConfiguredServerApplicationFactory()
    {
        _ownedDatabase = new SqliteIntegrationTestFixture();
        DatabasePath = _ownedDatabase.DatabasePath;
    }

    public ConfiguredServerApplicationFactory(string databasePath)
    {
        DatabasePath = Path.GetFullPath(databasePath);
    }

    public string DatabasePath { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> settings = new(TestServerSettings.Valid)
            {
                ["Database:Path"] = DatabasePath
            };
            configuration.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Interlocked.Exchange(ref _databaseDisposed, 1) == 0)
        {
            _ownedDatabase?.Dispose();
        }
    }
}
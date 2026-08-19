using System.Net;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Tests.Infrastructure.Configuration;

namespace SummerProject.Server.Tests.Infrastructure.Database;

public sealed class SqliteConnectionPolicyTests
{
    [Fact]
    public async Task EveryConnectionEnablesForeignKeysBusyTimeoutAndWal()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        using HttpClient client = application.CreateClient();
        using HttpResponseMessage health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        SqliteConnectionFactory connectionFactory =
            application.Services.GetRequiredService<SqliteConnectionFactory>();
        await using SqliteConnection first = await connectionFactory.OpenConnectionAsync();
        await using SqliteConnection second = await connectionFactory.OpenConnectionAsync();

        Assert.Equal(1, await first.QuerySingleAsync<int>("PRAGMA foreign_keys;"));
        Assert.Equal(5_000, await first.QuerySingleAsync<int>("PRAGMA busy_timeout;"));
        Assert.Equal(1, await second.QuerySingleAsync<int>("PRAGMA foreign_keys;"));
        Assert.Equal(5_000, await second.QuerySingleAsync<int>("PRAGMA busy_timeout;"));
        Assert.Equal("wal", await second.QuerySingleAsync<string>("PRAGMA journal_mode;"));
    }
}
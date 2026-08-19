using System.Net;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Infrastructure.Database;
using SummerProject.Server.Tests.Infrastructure.Configuration;
using SummerProject.Server.Tests.Infrastructure.Database;

namespace SummerProject.Server.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealthReturnsOk()
    {
        await using ConfiguredServerApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRootReturnsNotFound()
    {
        await using ConfiguredServerApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidMigrationStateReturnsUnavailableWithoutDatabaseDetails()
    {
        using SqliteIntegrationTestFixture database = new();
        await using ConfiguredServerApplicationFactory application = new(database.DatabasePath);
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage healthyResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthyResponse.StatusCode);

        SqliteConnectionFactory connectionFactory =
            application.Services.GetRequiredService<SqliteConnectionFactory>();
        await using (SqliteConnection connection = await connectionFactory.OpenConnectionAsync())
        {
            await connection.ExecuteAsync(
                "UPDATE schema_migrations SET checksum = @Checksum WHERE version = 1;",
                new { Checksum = new string('0', 64) });
        }

        using HttpResponseMessage unavailableResponse = await client.GetAsync("/health");
        string body = await unavailableResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailableResponse.StatusCode);
        Assert.DoesNotContain(database.DatabasePath, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("schema_migrations", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("체크섬", body, StringComparison.Ordinal);
    }
}
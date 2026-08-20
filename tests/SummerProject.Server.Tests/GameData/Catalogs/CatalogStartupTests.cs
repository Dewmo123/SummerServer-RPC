using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Tests.Infrastructure.Configuration;

namespace SummerProject.Server.Tests.GameData.Catalogs;

public sealed class CatalogStartupTests
{
    [Fact]
    public async Task ValidDeployedCatalogsLoadBeforeRequests()
    {
        await using ConfiguredServerApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, application.Services.GetRequiredService<MapCatalog>().Count);
        Assert.Equal(1, application.Services.GetRequiredService<StageCatalog>().Count);
    }

    [Fact]
    public async Task InvalidCatalogPreventsStartupBeforeDatabaseCreation()
    {
        using CatalogTestDirectory catalogs = new();
        catalogs.WriteRawStage("Stage1.json", "{ broken }");
        await using CatalogApplicationFactory application = new(catalogs.RootPath);

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using HttpClient client = application.CreateClient();
            await client.GetAsync("/health");
        });

        Assert.Contains("Stages/Stage1.json", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(catalogs.RootPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(application.DatabasePath));
    }

    private sealed class CatalogApplicationFactory(string catalogRootPath)
        : ConfiguredServerApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Catalog:RootPath"] = catalogRootPath
                    });
            });
        }
    }
}
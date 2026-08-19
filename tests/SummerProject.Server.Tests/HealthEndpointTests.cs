using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace SummerProject.Server.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealthReturnsOk()
    {
        await using WebApplicationFactory<Program> application = new();
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRootReturnsNotFound()
    {
        await using WebApplicationFactory<Program> application = new();
        using HttpClient client = application.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
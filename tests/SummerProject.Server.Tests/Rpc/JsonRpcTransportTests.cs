using System.Net;
using System.Text;
using System.Text.Json;

namespace SummerProject.Server.Tests.Rpc;

public sealed class JsonRpcTransportTests(JsonRpcTestApplicationFactory factory)
    : IClassFixture<JsonRpcTestApplicationFactory>
{
    [Theory]
    [InlineData(null)]
    [InlineData("text/plain")]
    [InlineData("application/problem+json")]
    [InlineData("application/json; charset=utf-16")]
    public async Task UnsupportedContentTypeReturns415(string? contentType)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.empty","id":1}""",
            contentType);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Utf8JsonContentTypeIsAccepted()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.empty","id":1}""",
            "application/json; charset=UTF-8");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRpcReturns405()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/rpc");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task RequestAtMaximumBodySizeIsAccepted()
    {
        const int maximumBytes = 65_536;
        const string request = "{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":1}";
        string paddedRequest = request + new string(' ', maximumBytes - Encoding.UTF8.GetByteCount(request));
        using HttpClient client = factory.CreateClient();

        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, paddedRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestAboveMaximumBodySizeReturns413()
    {
        const int maximumBytes = 65_536;
        string oversizedRequest = new(' ', maximumBytes + 1);
        using HttpClient client = factory.CreateClient();

        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, oversizedRequest);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(string.Empty, response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":")]
    [InlineData("[{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":1}")]
    public async Task InvalidJsonReturnsParseError(string json)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, json);

        JsonRpcAssertions.HasError(response, -32700, "RPC_PARSE_ERROR");
    }

    [Fact]
    public async Task RootPrimitiveReturnsInvalidRequest()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, "1");

        JsonRpcAssertions.HasError(response, -32600, "RPC_INVALID_REQUEST");
    }

    [Fact]
    public async Task JsonAtMaximumDepthIsAccepted()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            CreateNestedRequest(30));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JsonAboveMaximumDepthReturnsParseError()
    {
        using HttpClient client = factory.CreateClient();

        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            CreateNestedRequest(31));

        JsonRpcAssertions.HasError(response, -32700, "RPC_PARSE_ERROR");
    }

    [Fact]
    public async Task ErrorObjectUsesIntegerCodeAndConciseMessage()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"unknown","id":1}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement error = document.RootElement.GetProperty("error");
        Assert.Equal(JsonValueKind.Number, error.GetProperty("code").ValueKind);
        Assert.True(error.GetProperty("code").TryGetInt32(out _));
        Assert.Equal("Method not found", error.GetProperty("message").GetString());
    }

    private static string CreateNestedRequest(int arrayDepth)
    {
        string nestedValue = "0";
        for (int index = 0; index < arrayDepth; index++)
        {
            nestedValue = $"[{nestedValue}]";
        }

        return $"{{\"jsonrpc\":\"2.0\",\"method\":\"test.echo\",\"params\":{{\"value\":{nestedValue}}},\"id\":1}}";
    }
}
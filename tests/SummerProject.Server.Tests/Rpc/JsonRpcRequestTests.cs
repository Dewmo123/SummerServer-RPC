using System.Net;
using System.Text.Json;

namespace SummerProject.Server.Tests.Rpc;

public sealed class JsonRpcRequestTests(JsonRpcTestApplicationFactory factory)
    : IClassFixture<JsonRpcTestApplicationFactory>
{
    [Fact]
    public async Task NamedParamsReturnsResultOnly()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.echo","params":{"value":{"nested":true}},"id":1}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.ContentType, StringComparison.OrdinalIgnoreCase);

        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement root = document.RootElement;
        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.True(root.GetProperty("result").GetProperty("value").GetProperty("nested").GetBoolean());
        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.False(root.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task PositionalParamsUseCatalogOrder()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.add","params":[7,5],"id":2}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(12, document.RootElement.GetProperty("result").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task NamedParamsDoNotDependOnPropertyOrder()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.add","params":{"right":5,"left":7},"id":2}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(12, document.RootElement.GetProperty("result").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task OmittedParamsBindAsEmptyObject()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.empty","id":3}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.True(document.RootElement.GetProperty("result").GetProperty("completed").GetBoolean());
    }

    [Theory]
    [InlineData("\"request-1\"")]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("1.25")]
    [InlineData("1e100")]
    [InlineData("123456789012345678901234567890")]
    public async Task ResponsePreservesIdJsonTypeAndValue(string idJson)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            $$"""{"jsonrpc":"2.0","method":"test.empty","id":{{idJson}}}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(idJson, document.RootElement.GetProperty("id").GetRawText());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":true}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":[]}")]
    [InlineData("{\"jsonrpc\":1,\"method\":\"test.empty\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"1.0\",\"method\":\"test.empty\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.1\",\"method\":\"test.empty\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":1,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"rpc.internal\",\"id\":1}")]
    [InlineData("{\"Jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":1}")]
    public async Task InvalidRequestShapeReturnsInvalidRequest(string json)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, json);

        JsonRpcAssertions.HasError(response, -32600, "RPC_INVALID_REQUEST");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("\"value\"")]
    public async Task PrimitiveParamsReturnInvalidParams(string paramsJson)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            $$"""{"jsonrpc":"2.0","method":"test.echo","params":{{paramsJson}},"id":4}""");

        JsonRpcAssertions.HasError(response, -32602, "RPC_INVALID_PARAMS", "4");
    }

    [Theory]
    [InlineData("{\"Value\":1}")]
    [InlineData("{\"value\":1,\"unknown\":2}")]
    [InlineData("{}")]
    public async Task NamedParamsRequireExactKnownFields(string paramsJson)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            $$"""{"jsonrpc":"2.0","method":"test.echo","params":{{paramsJson}},"id":5}""");

        JsonRpcAssertions.HasError(response, -32602, "RPC_INVALID_PARAMS", "5");
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("[1,2,3]")]
    public async Task PositionalParamsRequireExactCount(string paramsJson)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            $$"""{"jsonrpc":"2.0","method":"test.add","params":{{paramsJson}},"id":6}""");

        JsonRpcAssertions.HasError(response, -32602, "RPC_INVALID_PARAMS", "6");
    }

    [Fact]
    public async Task MethodLookupIsCaseSensitive()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"Test.empty","id":7}""");

        JsonRpcAssertions.HasError(response, -32601, "RPC_METHOD_NOT_FOUND", "7");
    }

    [Fact]
    public async Task UnknownMethodReturnsMethodNotFound()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"unknown.method","id":"unknown"}""");

        JsonRpcAssertions.HasError(response, -32601, "RPC_METHOD_NOT_FOUND", "\"unknown\"");
    }

    [Fact]
    public async Task HandlerExceptionReturnsInternalErrorWithoutExceptionDetails()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.fail","id":8}""");

        JsonRpcAssertions.HasError(response, -32603, "RPC_INTERNAL_ERROR", "8");
        Assert.DoesNotContain("노출되면 안 되는", response.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("stack", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NullResultIsSerializedExplicitly()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.null","id":9}""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement root = document.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("result").ValueKind);
        Assert.False(root.TryGetProperty("error", out _));
    }
}
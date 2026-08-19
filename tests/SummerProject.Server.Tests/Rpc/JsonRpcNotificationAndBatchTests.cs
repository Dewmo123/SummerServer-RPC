using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace SummerProject.Server.Tests.Rpc;

public sealed class JsonRpcNotificationAndBatchTests(JsonRpcTestApplicationFactory factory)
    : IClassFixture<JsonRpcTestApplicationFactory>
{
    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"unknown.method\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.echo\",\"params\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"test.fail\"}")]
    public async Task NotificationsNeverReturnAResponse(string json)
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, json);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, response.Body);
    }

    [Fact]
    public async Task ExplicitNullIdIsNotANotification()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.empty","id":null}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("id").ValueKind);
    }

    [Fact]
    public async Task InvalidObjectWithoutIdIsNotANotification()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":1}""");

        JsonRpcAssertions.HasError(response, -32600, "RPC_INVALID_REQUEST");
    }

    [Fact]
    public async Task NotificationFailureLogsCodeWithoutParams()
    {
        await using JsonRpcTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"test.echo","params":{"secret":"must-not-log"}}""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        JsonRpcTestLogEntry entry = Assert.Single(
            application.Logs,
            log => log.Message.Contains("ErrorCode: -32602", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain("must-not-log", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BatchReturnsResponsesForEachCall()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """[{"jsonrpc":"2.0","method":"test.empty","id":1},{"jsonrpc":"2.0","method":"test.add","params":[2,3],"id":"two"}]""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        string[] ids = document.RootElement.EnumerateArray()
            .Select(element => element.GetProperty("id").GetRawText())
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["\"two\"", "1"], ids);
    }

    [Fact]
    public async Task BatchOmitsNotificationResponses()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """[{"jsonrpc":"2.0","method":"test.empty"},{"jsonrpc":"2.0","method":"test.empty","id":1}]""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(1, document.RootElement.GetArrayLength());
        Assert.Equal(1, document.RootElement[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task AllNotificationBatchReturnsNoContent()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """[{"jsonrpc":"2.0","method":"test.empty"},{"jsonrpc":"2.0","method":"test.fail"}]""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, response.Body);
    }

    [Fact]
    public async Task EmptyBatchReturnsSingleInvalidRequestObject()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, "[]");

        JsonRpcAssertions.HasError(response, -32600, "RPC_INVALID_REQUEST");
        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    [Fact]
    public async Task InvalidBatchElementProducesCorrespondingError()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """[1,{"jsonrpc":"2.0","method":"test.empty","id":2}]""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        JsonElement invalidResponse = document.RootElement[0];
        Assert.Equal(-32600, invalidResponse.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal(JsonValueKind.Null, invalidResponse.GetProperty("id").ValueKind);
        Assert.Equal(2, document.RootElement[1].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task FailureInBatchDoesNotPreventOtherCalls()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            """[{"jsonrpc":"2.0","method":"test.fail","id":1},{"jsonrpc":"2.0","method":"test.empty","id":2}]""");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(-32603, document.RootElement[0].GetProperty("error").GetProperty("code").GetInt32());
        Assert.True(document.RootElement[1].GetProperty("result").GetProperty("completed").GetBoolean());
    }

    [Fact]
    public async Task MaximumBatchSizeIsAccepted()
    {
        using HttpClient client = factory.CreateClient();
        string json = CreateBatch(50);
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(50, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task BatchAboveMaximumReturnsSingleInvalidRequest()
    {
        using HttpClient client = factory.CreateClient();
        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(client, CreateBatch(51));

        JsonRpcAssertions.HasError(response, -32600, "RPC_INVALID_REQUEST");
        using JsonDocument document = JsonDocument.Parse(response.Body);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    private static string CreateBatch(int count) =>
        $"[{string.Join(',', Enumerable.Range(1, count).Select(id => $"{{\"jsonrpc\":\"2.0\",\"method\":\"test.empty\",\"id\":{id}}}"))}]";
}
using System.Net.Http.Headers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SummerProject.Server.Tests.Infrastructure.Configuration;
using SummerProject.Server.Tests.Rpc;

namespace SummerProject.Server.Tests.Infrastructure.Logging;

public sealed class StructuredLoggingTests
{
    [Fact]
    public async Task ProductionLoggingUsesZLoggerConsoleProvider()
    {
        await using ConfiguredServerApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/health");

        IEnumerable<ILoggerProvider> providers = application.Services.GetServices<ILoggerProvider>();

        Assert.Contains(
            providers,
            provider => provider.GetType().Name is "ZLoggerConsoleLoggerProvider");
    }

    [Fact]
    public async Task RpcSummaryContainsRequiredFieldsWithoutSensitiveValues()
    {
        // 헤더·params·문자열 ID의 원문이 로그에 섞이지 않는지 한 요청에서 함께 검증한다.
        const string authorizationValue = "must-not-log-authorization";
        const string parameterValue = "must-not-log-parameter";
        const string rawRpcId = "must-not-log-rpc-id";
        await using JsonRpcTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationValue);

        JsonRpcHttpResponse response = await JsonRpcTestClient.PostAsync(
            client,
            $$"""{"jsonrpc":"2.0","method":"test.echo","params":{"value":"{{parameterValue}}"},"id":"{{rawRpcId}}"}""");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        JsonRpcTestLogEntry entry = Assert.Single(
            application.Logs,
            log => log.Category.EndsWith("JsonRpcDispatcher", StringComparison.Ordinal)
                && log.Message.Contains("outcome: success", StringComparison.Ordinal));
        Assert.Contains("traceId:", entry.Message, StringComparison.Ordinal);
        Assert.Contains("rpcId: string:", entry.Message, StringComparison.Ordinal);
        Assert.Contains("rpcMethod: test.echo", entry.Message, StringComparison.Ordinal);
        Assert.Contains("durationMs:", entry.Message, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(entry.Properties["traceId"])));
        Assert.StartsWith("string:", Assert.IsType<string>(entry.Properties["rpcId"]), StringComparison.Ordinal);
        Assert.Equal("test.echo", entry.Properties["rpcMethod"]);
        Assert.IsType<double>(entry.Properties["durationMs"]);
        Assert.Equal("success", entry.Properties["outcome"]);
        Assert.DoesNotContain(authorizationValue, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(parameterValue, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(rawRpcId, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailedRpcSummaryContainsOutcomeAndErrorCode()
    {
        await using JsonRpcTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();

        await JsonRpcTestClient.PostAsync(
            client,
            """{"jsonrpc":"2.0","method":"unknown.method","id":1}""");

        JsonRpcTestLogEntry entry = Assert.Single(
            application.Logs,
            log => log.Category.EndsWith("JsonRpcDispatcher", StringComparison.Ordinal)
                && log.Message.Contains("ErrorCode: -32601", StringComparison.Ordinal));
        Assert.Contains("outcome: error", entry.Message, StringComparison.Ordinal);
        Assert.Equal(-32601, entry.Properties["errorCode"]);
        Assert.Equal("error", entry.Properties["outcome"]);
    }
}
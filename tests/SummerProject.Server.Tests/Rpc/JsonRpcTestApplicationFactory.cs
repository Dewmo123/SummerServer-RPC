using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SummerProject.Server.Rpc.Dispatching;

namespace SummerProject.Server.Tests.Rpc;

public sealed class JsonRpcTestApplicationFactory : WebApplicationFactory<Program>
{
    internal ConcurrentQueue<JsonRpcTestLogEntry> Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new JsonRpcTestLoggerProvider(Logs));
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddJsonRpcMethod<EchoRequest, EchoResponse, EchoHandler>("test.echo", "value");
            services.AddJsonRpcMethod<AddRequest, AddResponse, AddHandler>("test.add", "left", "right");
            services.AddJsonRpcMethod<EmptyRequest, CompletedResponse, EmptyHandler>("test.empty");
            services.AddJsonRpcMethod<EmptyRequest, object?, NullResultHandler>("test.null");
            services.AddJsonRpcMethod<EmptyRequest, CompletedResponse, FailureHandler>("test.fail");
        });
    }
}

internal static class JsonRpcTestClient
{
    public static async Task<JsonRpcHttpResponse> PostAsync(
        HttpClient client,
        string json,
        string? contentType = "application/json; charset=utf-8")
    {
        using ByteArrayContent content = new(Encoding.UTF8.GetBytes(json));
        if (contentType is not null)
        {
            content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        using HttpResponseMessage response = await client.PostAsync("/rpc", content);
        string body = await response.Content.ReadAsStringAsync();
        return new JsonRpcHttpResponse(
            response.StatusCode,
            response.Content.Headers.ContentType?.ToString(),
            body);
    }
}

internal static class JsonRpcAssertions
{
    public static void HasError(
        JsonRpcHttpResponse response,
        int code,
        string key,
        string expectedIdJson = "null")
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement root = document.RootElement;
        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.False(root.TryGetProperty("result", out _));

        JsonElement error = root.GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetInt32());
        Assert.Equal(key, error.GetProperty("data").GetProperty("key").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("data").GetProperty("traceId").GetString()));
        Assert.Equal(expectedIdJson, root.GetProperty("id").GetRawText());
    }
}

internal sealed record JsonRpcHttpResponse(
    HttpStatusCode StatusCode,
    string? ContentType,
    string Body);

public sealed class EchoRequest
{
    public required JsonElement Value { get; init; }
}

public sealed record EchoResponse(JsonElement Value);

public sealed class EchoHandler : IRpcMethodHandler<EchoRequest, EchoResponse>
{
    public ValueTask<EchoResponse> HandleAsync(
        EchoRequest request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new EchoResponse(request.Value));
}

public sealed class AddRequest
{
    public required int Left { get; init; }

    public required int Right { get; init; }
}

public sealed record AddResponse(int Total);

public sealed class AddHandler : IRpcMethodHandler<AddRequest, AddResponse>
{
    public ValueTask<AddResponse> HandleAsync(
        AddRequest request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new AddResponse(request.Left + request.Right));
}

public sealed class EmptyRequest;

public sealed record CompletedResponse(bool Completed);

public sealed class EmptyHandler : IRpcMethodHandler<EmptyRequest, CompletedResponse>
{
    public ValueTask<CompletedResponse> HandleAsync(
        EmptyRequest request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new CompletedResponse(true));
}

public sealed class NullResultHandler : IRpcMethodHandler<EmptyRequest, object?>
{
    public ValueTask<object?> HandleAsync(
        EmptyRequest request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult<object?>(null);
}

public sealed class FailureHandler : IRpcMethodHandler<EmptyRequest, CompletedResponse>
{
    public ValueTask<CompletedResponse> HandleAsync(
        EmptyRequest request,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("외부 응답에 노출되면 안 되는 내부 오류입니다.");
}

internal sealed record JsonRpcTestLogEntry(LogLevel Level, string Category, string Message);

internal sealed class JsonRpcTestLoggerProvider(ConcurrentQueue<JsonRpcTestLogEntry> logs) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new JsonRpcTestLogger(categoryName, logs);

    public void Dispose()
    {
    }
}

internal sealed class JsonRpcTestLogger(
    string category,
    ConcurrentQueue<JsonRpcTestLogEntry> logs) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        logs.Enqueue(new JsonRpcTestLogEntry(logLevel, category, formatter(state, exception)));
    }
}
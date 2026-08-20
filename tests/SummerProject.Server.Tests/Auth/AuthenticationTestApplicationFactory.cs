using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Tests.Infrastructure.Configuration;
using SummerProject.Server.Tests.Rpc;

namespace SummerProject.Server.Tests.Auth;

internal sealed class AuthenticationTestApplicationFactory(
    string environment = "Production",
    bool developmentLoginEnabled = false,
    string developmentUsername = "developer") : ConfiguredServerApplicationFactory
{
    public FakeGoogleIdTokenValidator GoogleValidator { get; } = new();

    public ConcurrentQueue<JsonRpcTestLogEntry> Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevelopmentLogin:Enabled"] = developmentLoginEnabled.ToString(),
                ["DevelopmentLogin:Username"] = developmentUsername
            });
        });
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new JsonRpcTestLoggerProvider(Logs));
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleIdTokenValidator>();
            services.AddSingleton<IGoogleIdTokenValidator>(GoogleValidator);
            services.AddAuthenticatedJsonRpcMethod<
                ProtectedRequest,
                ProtectedResponse,
                ProtectedHandler>("test.protected");
        });
    }
}

internal sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public ValueTask<GoogleIdentityProto> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!idToken.StartsWith("valid:", StringComparison.Ordinal)
            || idToken.Length == "valid:".Length)
        {
            throw new InvalidGoogleTokenException();
        }

        return ValueTask.FromResult(new GoogleIdentityProto(idToken["valid:".Length..]));
    }
}

internal static class AuthRpcClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static async Task<JsonDocument> PostAsync(
        HttpClient client,
        string method,
        object? parameters = null,
        string? bearerToken = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/rpc");
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        object envelope = parameters is null
            ? new { jsonrpc = "2.0", method, id = 1 }
            : new { jsonrpc = "2.0", method, @params = parameters, id = 1 };
        request.Content = new StringContent(
            JsonSerializer.Serialize(envelope, SerializerOptions),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(body);
    }

    public static string ReadAccessToken(JsonElement result) =>
        result.GetProperty("tokens").GetProperty("accessToken").GetString()!;

    public static string ReadRefreshToken(JsonElement result) =>
        result.GetProperty("tokens").GetProperty("refreshToken").GetString()!;

    public static void HasError(JsonDocument document, int code, string key)
    {
        JsonElement error = document.RootElement.GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetInt32());
        Assert.Equal(key, error.GetProperty("data").GetProperty("key").GetString());
    }
}

internal sealed class ProtectedRequest;

internal sealed record ProtectedResponse(long UserId, string Username);

internal sealed class ProtectedHandler(CallerContext callerContext)
    : IRpcMethodHandler<ProtectedRequest, ProtectedResponse>
{
    public ValueTask<ProtectedResponse> HandleAsync(
        ProtectedRequest request,
        CancellationToken cancellationToken)
    {
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        return ValueTask.FromResult(new ProtectedResponse(caller.UserId, caller.Username));
    }
}
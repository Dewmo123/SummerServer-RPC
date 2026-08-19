using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Bootstrap;

internal static class ServiceRegistration
{
    public static IServiceCollection AddServerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks();

        services
            .AddOptions<JsonRpcOptions>()
            .Bind(configuration.GetSection(JsonRpcOptions.SectionName))
            .Validate(options => options.MaxRequestBodyBytes > 0, "JSON-RPC 요청 본문 제한은 1 이상이어야 합니다.")
            .Validate(options => options.MaxBatchSize > 0, "JSON-RPC 배치 제한은 1 이상이어야 합니다.")
            .Validate(options => options.MaxJsonDepth > 0, "JSON 최대 깊이는 1 이상이어야 합니다.")
            .ValidateOnStart();

        services.AddSingleton<JsonRpcSerializerOptions>();
        services.AddSingleton<JsonRpcRequestParser>();
        services.AddSingleton<JsonRpcParameterBinder>();
        services.AddSingleton<JsonRpcMethodRegistry>();
        services.AddSingleton<JsonRpcResponseWriter>();
        services.AddSingleton<JsonRpcExceptionMapper>();
        services.AddScoped<JsonRpcDispatcher>();
        services.AddScoped<JsonRpcRequestProcessor>();

        return services;
    }
}
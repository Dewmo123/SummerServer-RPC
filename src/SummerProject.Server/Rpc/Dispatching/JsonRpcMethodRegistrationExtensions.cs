using System.Text.Json;

using Microsoft.Extensions.DependencyInjection.Extensions;

using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

/// <summary>
/// 업무 Handler를 대소문자를 구분하는 JSON-RPC 메서드명에 연결합니다.
/// </summary>
public static class JsonRpcMethodRegistrationExtensions
{
    public static IServiceCollection AddJsonRpcMethod<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        string methodName,
        params string[] parameterNames)
        where THandler : class, IRpcMethodHandler<TRequest, TResponse>
        => AddJsonRpcMethod<TRequest, TResponse, THandler>(
            services,
            methodName,
            requiresAuthentication: false,
            availability: null,
            parameterNames);

    /// <summary>
    /// 유효한 JWT 호출자만 실행할 수 있는 JSON-RPC 메서드를 등록합니다.
    /// </summary>
    public static IServiceCollection AddAuthenticatedJsonRpcMethod<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        string methodName,
        params string[] parameterNames)
        where THandler : class, IRpcMethodHandler<TRequest, TResponse>
        => AddJsonRpcMethod<TRequest, TResponse, THandler>(
            services,
            methodName,
            requiresAuthentication: true,
            availability: null,
            parameterNames);

    internal static IServiceCollection AddConditionalJsonRpcMethod<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        string methodName,
        Func<IServiceProvider, bool> availability,
        params string[] parameterNames)
        where THandler : class, IRpcMethodHandler<TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(availability);
        return AddJsonRpcMethod<TRequest, TResponse, THandler>(
            services,
            methodName,
            requiresAuthentication: false,
            availability,
            parameterNames);
    }

    private static IServiceCollection AddJsonRpcMethod<TRequest, TResponse, THandler>(
        IServiceCollection services,
        string methodName,
        bool requiresAuthentication,
        Func<IServiceProvider, bool>? availability,
        string[] parameterNames)
        where THandler : class, IRpcMethodHandler<TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        // rpc. 접두사는 JSON-RPC 시스템 확장을 위해 예약되어 애플리케이션 메서드에 사용할 수 없다.
        if (string.IsNullOrEmpty(methodName) || methodName.StartsWith("rpc.", StringComparison.Ordinal))
        {
            throw new ArgumentException("애플리케이션 JSON-RPC 메서드명이 유효하지 않습니다.", nameof(methodName));
        }

        ArgumentNullException.ThrowIfNull(parameterNames);
        if (parameterNames.Any(string.IsNullOrEmpty)
            || parameterNames.Distinct(StringComparer.Ordinal).Count() != parameterNames.Length)
        {
            throw new ArgumentException("params 필드명은 비어 있지 않고 중복되지 않아야 합니다.", nameof(parameterNames));
        }

        services.TryAddTransient<THandler>();
        services.AddSingleton<IJsonRpcMethodDefinition>(
            new JsonRpcMethodDefinition<TRequest, TResponse, THandler>(
                methodName,
                parameterNames.ToArray(),
                requiresAuthentication,
                availability));
        return services;
    }
}

internal interface IJsonRpcMethodDefinition
{
    string MethodName { get; }

    bool IsAvailable(IServiceProvider serviceProvider);

    ValueTask<JsonElement> InvokeAsync(
        JsonElement? parameters,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class JsonRpcMethodDefinition<TRequest, TResponse, THandler>(
    string methodName,
    IReadOnlyList<string> parameterNames,
    bool requiresAuthentication,
    Func<IServiceProvider, bool>? availability) : IJsonRpcMethodDefinition
    where THandler : class, IRpcMethodHandler<TRequest, TResponse>
{
    public string MethodName { get; } = methodName;

    public bool IsAvailable(IServiceProvider serviceProvider) =>
        availability?.Invoke(serviceProvider) ?? true;

    public async ValueTask<JsonElement> InvokeAsync(
        JsonElement? parameters,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (requiresAuthentication
            && serviceProvider.GetRequiredService<CallerContext>().Caller is null)
        {
            throw new UnauthenticatedCallerException();
        }

        JsonRpcParameterBinder binder = serviceProvider.GetRequiredService<JsonRpcParameterBinder>();
        TRequest request = binder.Bind<TRequest>(parameters, parameterNames);
        THandler handler = serviceProvider.GetRequiredService<THandler>();
        TResponse result = await handler.HandleAsync(request, cancellationToken);
        JsonRpcResponse<TResponse> response = new(result, JsonRpcIdProto.Missing);
        JsonRpcSerializerOptions serializerOptions = serviceProvider.GetRequiredService<JsonRpcSerializerOptions>();
        return JsonSerializer.SerializeToElement(response.Result, serializerOptions.Value);
    }
}
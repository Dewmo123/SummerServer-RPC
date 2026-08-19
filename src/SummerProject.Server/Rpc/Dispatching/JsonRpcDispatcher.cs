using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Serialization;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcDispatcher(
    JsonRpcMethodRegistry registry,
    JsonRpcExceptionMapper exceptionMapper,
    ILogger<JsonRpcDispatcher> logger)
{
    public async ValueTask<JsonRpcResponseEnvelope?> DispatchAsync(
        JsonRpcRequest request,
        IServiceProvider serviceProvider,
        string traceId,
        CancellationToken cancellationToken)
    {
        JsonRpcResponseEnvelope response;

        if (!registry.TryGetMethod(request.Method, out IJsonRpcMethodDefinition? definition))
        {
            response = JsonRpcResponseEnvelope.Failure(
                request.Id,
                JsonRpcErrors.MethodNotFound(traceId));
        }
        else
        {
            try
            {
                System.Text.Json.JsonElement result = await definition!.InvokeAsync(
                    request.Parameters,
                    serviceProvider,
                    cancellationToken);
                response = JsonRpcResponseEnvelope.Success(request.Id, result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                response = JsonRpcResponseEnvelope.Failure(
                    request.Id,
                    exceptionMapper.Map(exception, traceId));
            }
        }

        if (!request.IsNotification)
        {
            return response;
        }

        if (response.IsError)
        {
            logger.LogWarning(
                "JSON-RPC 알림 처리에 실패했습니다. RpcMethod: {RpcMethod}, ErrorCode: {ErrorCode}",
                request.Method,
                response.Error!.Code);
        }

        return null;
    }
}
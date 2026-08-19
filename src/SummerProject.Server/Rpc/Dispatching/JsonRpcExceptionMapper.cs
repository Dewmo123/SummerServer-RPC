using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcExceptionMapper(ILogger<JsonRpcExceptionMapper> logger)
{
    public JsonRpcErrorPacket Map(Exception exception, string traceId)
    {
        if (exception is JsonRpcInvalidParamsException)
        {
            return JsonRpcErrors.InvalidParams(traceId);
        }

        logger.LogError(
            "JSON-RPC 처리 중 예기치 않은 예외가 발생했습니다. ExceptionType: {ExceptionType}",
            exception.GetType().FullName);
        return JsonRpcErrors.InternalError(traceId);
    }
}
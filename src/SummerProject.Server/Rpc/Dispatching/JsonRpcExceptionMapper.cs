using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcExceptionMapper
{
    public JsonRpcErrorPacket Map(Exception exception, string traceId)
    {
        if (exception is JsonRpcInvalidParamsException)
        {
            return JsonRpcErrors.InvalidParams(traceId);
        }

        // 예상하지 못한 예외의 세부 정보는 클라이언트 계약에 노출하지 않는다.
        return JsonRpcErrors.InternalError(traceId);
    }
}
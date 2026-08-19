using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Serialization;

namespace SummerProject.Server.Rpc.Validation;

internal sealed record JsonRpcParseResult(
    bool WriteResponseAsBatch,
    IReadOnlyList<JsonRpcWorkItem> Items);

internal sealed class JsonRpcWorkItem
{
    private JsonRpcWorkItem(
        JsonRpcRequest? request,
        JsonRpcResponseEnvelope? errorResponse,
        bool suppressResponse,
        string? method)
    {
        Request = request;
        ErrorResponse = errorResponse;
        SuppressResponse = suppressResponse;
        Method = method;
    }

    public JsonRpcRequest? Request { get; }

    public JsonRpcResponseEnvelope? ErrorResponse { get; }

    // 알림에서 발생한 검증 오류는 기록하되 응답 목록에는 포함하지 않는다.
    public bool SuppressResponse { get; }

    public string? Method { get; }

    public static JsonRpcWorkItem FromRequest(JsonRpcRequest request) =>
        new(request, null, false, request.Method);

    public static JsonRpcWorkItem FromError(
        JsonRpcResponseEnvelope errorResponse,
        bool suppressResponse = false,
        string? method = null) =>
        new(null, errorResponse, suppressResponse, method);
}
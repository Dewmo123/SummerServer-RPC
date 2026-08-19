namespace SummerProject.Server.Rpc.Contracts;

// 표준 오류 코드와 클라이언트 분기용 key를 한곳에서 고정한다.
internal static class JsonRpcErrors
{
    public static JsonRpcErrorPacket ParseError(string traceId) =>
        Create(-32700, "Parse error", "RPC_PARSE_ERROR", traceId);

    public static JsonRpcErrorPacket InvalidRequest(string traceId) =>
        Create(-32600, "Invalid Request", "RPC_INVALID_REQUEST", traceId);

    public static JsonRpcErrorPacket MethodNotFound(string traceId) =>
        Create(-32601, "Method not found", "RPC_METHOD_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket InvalidParams(string traceId) =>
        Create(-32602, "Invalid params", "RPC_INVALID_PARAMS", traceId);

    public static JsonRpcErrorPacket InternalError(string traceId) =>
        Create(-32603, "Internal error", "RPC_INTERNAL_ERROR", traceId);

    private static JsonRpcErrorPacket Create(int code, string message, string key, string traceId) =>
        new(code, message, new JsonRpcErrorDataPacket(key, traceId));
}
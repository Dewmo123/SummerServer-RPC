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

    public static JsonRpcErrorPacket Unauthenticated(string traceId) =>
        Create(-32001, "인증이 필요합니다.", "AUTH_UNAUTHENTICATED", traceId);

    public static JsonRpcErrorPacket InvalidGoogleToken(string traceId) =>
        Create(1001, "Google 인증 정보가 유효하지 않습니다.", "AUTH_INVALID_GOOGLE_TOKEN", traceId);

    public static JsonRpcErrorPacket InvalidRefreshToken(string traceId) =>
        Create(1002, "리프레시 토큰이 유효하지 않거나 만료되었습니다.", "AUTH_INVALID_REFRESH_TOKEN", traceId);

    public static JsonRpcErrorPacket RefreshTokenReused(string traceId) =>
        Create(1003, "토큰 재사용이 감지되어 세션을 폐기했습니다.", "AUTH_REFRESH_TOKEN_REUSED", traceId);

    public static JsonRpcErrorPacket DevelopmentUserNotFound(string traceId) =>
        Create(1004, "개발 사용자를 찾을 수 없습니다.", "AUTH_DEVELOPMENT_USER_NOT_FOUND", traceId);

    private static JsonRpcErrorPacket Create(int code, string message, string key, string traceId) =>
        new(code, message, new JsonRpcErrorDataPacket(key, traceId));
}
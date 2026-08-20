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

    public static JsonRpcErrorPacket UserNotFound(string traceId) =>
        Create(1101, "사용자를 찾을 수 없습니다.", "USER_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket CharacterNotFound(string traceId) =>
        Create(1201, "캐릭터를 찾을 수 없습니다.", "CHARACTER_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket CharacterInvalidExperience(string traceId) =>
        Create(1202, "지급 경험치가 유효하지 않습니다.", "CHARACTER_INVALID_EXPERIENCE", traceId);

    public static JsonRpcErrorPacket CurrencyInvalidType(string traceId) =>
        Create(1301, "지원하지 않는 재화 종류입니다.", "CURRENCY_INVALID_TYPE", traceId);

    public static JsonRpcErrorPacket CurrencyInsufficient(string traceId) =>
        Create(1302, "재화가 부족합니다.", "CURRENCY_INSUFFICIENT", traceId);

    public static JsonRpcErrorPacket CurrencyInvalidAmount(string traceId) =>
        Create(1303, "재화 변경량이 유효하지 않습니다.", "CURRENCY_INVALID_AMOUNT", traceId);

    public static JsonRpcErrorPacket CurrencyOverflow(string traceId) =>
        Create(1304, "재화 한도를 초과합니다.", "CURRENCY_OVERFLOW", traceId);

    public static JsonRpcErrorPacket StageNotFound(string traceId) =>
        Create(1401, "존재하지 않는 스테이지입니다.", "STAGE_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket StageRunNotFound(string traceId) =>
        Create(1402, "스테이지 실행 기록을 찾을 수 없습니다.", "STAGE_RUN_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket StageRunForbidden(string traceId) =>
        Create(1403, "다른 사용자의 실행을 완료할 수 없습니다.", "STAGE_RUN_FORBIDDEN", traceId);

    public static JsonRpcErrorPacket StageRunAlreadyCompleted(string traceId) =>
        Create(1404, "이미 처리된 스테이지 실행입니다.", "STAGE_RUN_ALREADY_COMPLETED", traceId);

    public static JsonRpcErrorPacket StageClearTooEarly(string traceId) =>
        Create(1405, "최소 클리어 시간이 지나지 않았습니다.", "STAGE_CLEAR_TOO_EARLY", traceId);

    public static JsonRpcErrorPacket StageRewardFailed(string traceId) =>
        Create(1406, "보상 지급에 실패했습니다.", "STAGE_REWARD_FAILED", traceId);

    public static JsonRpcErrorPacket MapNotFound(string traceId) =>
        Create(1501, "존재하지 않는 맵입니다.", "MAP_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket RoomNotFound(string traceId) =>
        Create(1502, "저장된 방이 없습니다.", "ROOM_NOT_FOUND", traceId);

    public static JsonRpcErrorPacket RoomMapInvalid(string traceId) =>
        Create(1503, "저장된 방의 맵 정보가 유효하지 않습니다.", "ROOM_MAP_INVALID", traceId);

    public static JsonRpcErrorPacket TrapTypeUnsupported(string traceId) =>
        Create(1504, "지원하지 않는 함정 종류입니다.", "TRAP_TYPE_UNSUPPORTED", traceId);

    public static JsonRpcErrorPacket TrapOutOfBounds(string traceId) =>
        Create(1505, "함정 좌표가 맵 범위를 벗어났습니다.", "TRAP_OUT_OF_BOUNDS", traceId);

    public static JsonRpcErrorPacket TrapPositionDuplicated(string traceId) =>
        Create(1506, "같은 위치에 함정을 중복 배치할 수 없습니다.", "TRAP_POSITION_DUPLICATED", traceId);

    public static JsonRpcErrorPacket TrapRotationInvalid(string traceId) =>
        Create(1507, "함정 회전값이 정규화되어 있지 않습니다.", "TRAP_ROTATION_INVALID", traceId);

    private static JsonRpcErrorPacket Create(int code, string message, string key, string traceId) =>
        new(code, message, new JsonRpcErrorDataPacket(key, traceId));
}
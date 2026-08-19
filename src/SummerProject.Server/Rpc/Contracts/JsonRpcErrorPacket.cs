namespace SummerProject.Server.Rpc.Contracts;

/// <summary>
/// JSON-RPC error의 안정적인 코드와 추적 정보를 표현합니다.
/// </summary>
public sealed record JsonRpcErrorPacket(
    int Code,
    string Message,
    JsonRpcErrorDataPacket Data);

/// <summary>
/// 클라이언트 분기용 오류 key와 운영 추적 ID를 표현합니다.
/// </summary>
public sealed record JsonRpcErrorDataPacket(string Key, string TraceId);
using System.Text.Json;

namespace SummerProject.Server.Rpc.Contracts;

/// <summary>
/// 구조 검증을 통과한 JSON-RPC 요청 봉투입니다.
/// </summary>
public sealed record JsonRpcRequest(
    string Method,
    JsonElement? Parameters,
    JsonRpcIdProto Id)
{
    public bool IsNotification => !Id.IsPresent;
}
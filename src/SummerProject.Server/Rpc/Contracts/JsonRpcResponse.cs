namespace SummerProject.Server.Rpc.Contracts;

/// <summary>
/// 성공한 JSON-RPC 호출의 result와 요청 ID를 함께 보관합니다.
/// </summary>
/// <typeparam name="TResponse">업무 메서드의 응답 타입입니다.</typeparam>
public sealed record JsonRpcResponse<TResponse>(TResponse Result, JsonRpcIdProto Id);
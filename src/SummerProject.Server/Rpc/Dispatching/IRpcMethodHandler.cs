namespace SummerProject.Server.Rpc.Dispatching;

/// <summary>
/// JSON-RPC params 요청을 업무 응답으로 변환하는 메서드 Handler 계약입니다.
/// </summary>
/// <typeparam name="TRequest">메서드 params를 표현하는 요청 타입입니다.</typeparam>
/// <typeparam name="TResponse">메서드 result를 표현하는 응답 타입입니다.</typeparam>
public interface IRpcMethodHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
using System.Diagnostics;

using SummerProject.Server.Infrastructure.Logging;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Serialization;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcDispatcher(
    JsonRpcMethodRegistry registry,
    JsonRpcExceptionMapper exceptionMapper,
    JsonRpcLogWriter logWriter,
    CallerContext callerContext,
    ILogger<JsonRpcDispatcher> logger)
{
    public async ValueTask<JsonRpcResponseEnvelope?> DispatchAsync(
        JsonRpcRequest request,
        IServiceProvider serviceProvider,
        string traceId,
        CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();
        JsonRpcResponseEnvelope response;
        string? exceptionType = null;

        if (!registry.TryGetMethod(request.Method, out IJsonRpcMethodDefinition? definition))
        {
            response = JsonRpcResponseEnvelope.Failure(
                request.Id,
                JsonRpcErrors.MethodNotFound(traceId));
        }
        else
        {
            try
            {
                System.Text.Json.JsonElement result = await definition!.InvokeAsync(
                    request.Parameters,
                    serviceProvider,
                    cancellationToken);
                response = JsonRpcResponseEnvelope.Success(request.Id, result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 요청 취소는 서버 오류가 아니므로 내부 오류 응답이나 경고 로그로 변환하지 않는다.
                throw;
            }
            catch (Exception exception)
            {
                exceptionType = exception.GetType().FullName;
                response = JsonRpcResponseEnvelope.Failure(
                    request.Id,
                    exceptionMapper.Map(exception, traceId));
            }
        }

        string outcome = request.IsNotification
            ? "notification"
            : response.IsError ? "error" : "success";
        logWriter.Write(
            logger,
            traceId,
            request.Id,
            request.Method,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            outcome,
            response.Error?.Code,
            exceptionType,
            callerContext.Caller?.UserId);

        // 알림도 처리 결과는 기록하지만 JSON-RPC 규격에 따라 응답 본문은 만들지 않는다.
        return request.IsNotification ? null : response;
    }
}
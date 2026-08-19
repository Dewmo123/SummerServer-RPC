using Microsoft.Extensions.Logging;

using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Infrastructure.Logging;

// 원문 요청 대신 운영 추적에 필요한 안전한 요약 필드만 구조화 로그로 남긴다.
internal sealed class JsonRpcLogWriter(SensitiveLogFilter sensitiveLogFilter)
{
    public void Write(
        ILogger logger,
        string traceId,
        JsonRpcIdProto id,
        string? method,
        double durationMs,
        string outcome,
        int? errorCode = null,
        string? exceptionType = null,
        long? userId = null)
    {
        string safeTraceId = Filter("traceId", traceId);
        string safeRpcId = Filter("rpcId", id.ToSafeLogValue());
        string safeMethod = Filter("rpcMethod", method ?? "(unknown)");

        if (errorCode is null)
        {
            if (userId is null)
            {
                logger.LogInformation(
                    "JSON-RPC 요청 처리가 완료되었습니다. traceId: {traceId}, rpcId: {rpcId}, rpcMethod: {rpcMethod}, durationMs: {durationMs}, outcome: {outcome}",
                    safeTraceId,
                    safeRpcId,
                    safeMethod,
                    durationMs,
                    outcome);
            }
            else
            {
                logger.LogInformation(
                    "JSON-RPC 요청 처리가 완료되었습니다. traceId: {traceId}, rpcId: {rpcId}, rpcMethod: {rpcMethod}, userId: {userId}, durationMs: {durationMs}, outcome: {outcome}",
                    safeTraceId,
                    safeRpcId,
                    safeMethod,
                    userId,
                    durationMs,
                    outcome);
            }

            return;
        }

        string message = outcome == "notification"
            ? "JSON-RPC 알림 처리에 실패했습니다."
            : "JSON-RPC 요청 처리에 실패했습니다.";

        // 예외 메시지는 내부 정보가 포함될 수 있으므로 형식 이름만 선택적으로 기록한다.
        if (exceptionType is null && userId is null)
        {
            logger.LogWarning(
                "{summary} RpcMethod: {rpcMethod}, ErrorCode: {errorCode}, traceId: {traceId}, rpcId: {rpcId}, durationMs: {durationMs}, outcome: {outcome}",
                message,
                safeMethod,
                errorCode,
                safeTraceId,
                safeRpcId,
                durationMs,
                outcome);
        }
        else if (exceptionType is null)
        {
            logger.LogWarning(
                "{summary} RpcMethod: {rpcMethod}, ErrorCode: {errorCode}, traceId: {traceId}, rpcId: {rpcId}, userId: {userId}, durationMs: {durationMs}, outcome: {outcome}",
                message,
                safeMethod,
                errorCode,
                safeTraceId,
                safeRpcId,
                userId,
                durationMs,
                outcome);
        }
        else if (userId is null)
        {
            logger.LogWarning(
                "{summary} RpcMethod: {rpcMethod}, ErrorCode: {errorCode}, traceId: {traceId}, rpcId: {rpcId}, durationMs: {durationMs}, outcome: {outcome}, exceptionType: {exceptionType}",
                message,
                safeMethod,
                errorCode,
                safeTraceId,
                safeRpcId,
                durationMs,
                outcome,
                Filter("exceptionType", exceptionType));
        }
        else
        {
            logger.LogWarning(
                "{summary} RpcMethod: {rpcMethod}, ErrorCode: {errorCode}, traceId: {traceId}, rpcId: {rpcId}, userId: {userId}, durationMs: {durationMs}, outcome: {outcome}, exceptionType: {exceptionType}",
                message,
                safeMethod,
                errorCode,
                safeTraceId,
                safeRpcId,
                userId,
                durationMs,
                outcome,
                Filter("exceptionType", exceptionType));
        }
    }

    private string Filter(string propertyName, string value)
    {
        if (!sensitiveLogFilter.TryFilter(propertyName, value, out string? safeValue))
        {
            throw new InvalidOperationException($"허용되지 않은 로그 필드입니다: {propertyName}");
        }

        return safeValue ?? string.Empty;
    }
}
using System.Diagnostics;

using SummerProject.Server.Infrastructure.Logging;
using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcRequestProcessor(
    JsonRpcRequestParser parser,
    JsonRpcDispatcher dispatcher,
    JsonRpcResponseWriter responseWriter,
    JsonRpcLogWriter logWriter,
    CallerContext callerContext,
    ILogger<JsonRpcRequestProcessor> logger)
{
    public async ValueTask<byte[]?> ProcessAsync(
        ReadOnlyMemory<byte> json,
        IServiceProvider serviceProvider,
        string traceId,
        CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();
        JsonRpcParseResult parseResult = parser.Parse(json, traceId);
        List<JsonRpcResponseEnvelope> responses = [];

        foreach (JsonRpcWorkItem item in parseResult.Items)
        {
            if (item.ErrorResponse is not null)
            {
                // 잘못된 알림은 응답하지 않지만 운영 추적을 위해 실패 요약은 남긴다.
                logWriter.Write(
                    logger,
                    traceId,
                    item.ErrorResponse.Id,
                    item.Method,
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                    item.SuppressResponse ? "notification" : "error",
                    item.ErrorResponse.Error!.Code,
                    userId: callerContext.Caller?.UserId);

                if (!item.SuppressResponse)
                {
                    responses.Add(item.ErrorResponse);
                }

                continue;
            }

            JsonRpcResponseEnvelope? response = await dispatcher.DispatchAsync(
                item.Request!,
                serviceProvider,
                traceId,
                cancellationToken);
            if (response is not null)
            {
                responses.Add(response);
            }
        }

        return responses.Count == 0
            ? null
            : responseWriter.Write(responses, parseResult.WriteResponseAsBatch);
    }
}
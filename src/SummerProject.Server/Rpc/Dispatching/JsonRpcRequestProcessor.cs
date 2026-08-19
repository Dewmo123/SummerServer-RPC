using SummerProject.Server.Rpc.Serialization;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcRequestProcessor(
    JsonRpcRequestParser parser,
    JsonRpcDispatcher dispatcher,
    JsonRpcResponseWriter responseWriter,
    ILogger<JsonRpcRequestProcessor> logger)
{
    public async ValueTask<byte[]?> ProcessAsync(
        ReadOnlyMemory<byte> json,
        IServiceProvider serviceProvider,
        string traceId,
        CancellationToken cancellationToken)
    {
        JsonRpcParseResult parseResult = parser.Parse(json, traceId);
        List<JsonRpcResponseEnvelope> responses = [];

        foreach (JsonRpcWorkItem item in parseResult.Items)
        {
            if (item.ErrorResponse is not null)
            {
                if (item.SuppressResponse)
                {
                    logger.LogWarning(
                        "JSON-RPC 알림 검증에 실패했습니다. RpcMethod: {RpcMethod}, ErrorCode: {ErrorCode}",
                        item.Method,
                        item.ErrorResponse.Error!.Code);
                }
                else
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
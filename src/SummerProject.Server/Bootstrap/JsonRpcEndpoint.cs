using System.Buffers;
using System.Diagnostics;

using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Dispatching;

namespace SummerProject.Server.Bootstrap;

internal static class JsonRpcEndpoint
{
    private const string JsonContentType = "application/json";

    public static async Task HandleAsync(
        HttpContext context,
        JsonRpcRequestProcessor processor,
        IOptions<JsonRpcOptions> options)
    {
        if (!HasSupportedContentType(context.Request.ContentType))
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        int maxRequestBodyBytes = options.Value.MaxRequestBodyBytes;
        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        byte[]? body = await ReadBodyAsync(
            context.Request.Body,
            maxRequestBodyBytes,
            context.RequestAborted);
        if (body is null)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        string traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        byte[]? response = await processor.ProcessAsync(
            body,
            context.RequestServices,
            traceId,
            context.RequestAborted);

        if (response is null)
        {
            // 알림 요청은 성공과 실패 모두 응답을 만들지 않는다.
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength = response.Length;
        await context.Response.Body.WriteAsync(response, context.RequestAborted);
    }

    private static bool HasSupportedContentType(string? value)
    {
        if (!MediaTypeHeaderValue.TryParse(value, out MediaTypeHeaderValue? contentType)
            || !string.Equals(contentType.MediaType.Value, JsonContentType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!contentType.Charset.HasValue)
        {
            return true;
        }

        string charset = contentType.Charset.Value.Trim('"');
        return string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase)
            || string.Equals(charset, "utf8", StringComparison.OrdinalIgnoreCase);
    }

    private static async ValueTask<byte[]?> ReadBodyAsync(
        Stream body,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        // Content-Length가 없는 청크 요청도 실제 읽은 바이트 수로 제한한다.
        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(8_192);

        try
        {
            using MemoryStream destination = new();
            int totalBytes = 0;

            while (true)
            {
                int remainingBytes = maximumBytes - totalBytes;
                int requestedBytes = remainingBytes >= readBuffer.Length
                    ? readBuffer.Length
                    : remainingBytes + 1;
                int bytesRead = await body.ReadAsync(
                    readBuffer.AsMemory(0, requestedBytes),
                    cancellationToken);
                if (bytesRead == 0)
                {
                    return destination.ToArray();
                }

                totalBytes += bytesRead;
                if (totalBytes > maximumBytes)
                {
                    return null;
                }

                await destination.WriteAsync(
                    readBuffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }
}
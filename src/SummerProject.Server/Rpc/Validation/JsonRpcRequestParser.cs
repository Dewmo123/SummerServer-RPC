using System.Text.Json;

using Microsoft.Extensions.Options;

using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Serialization;

namespace SummerProject.Server.Rpc.Validation;

internal sealed class JsonRpcRequestParser(IOptions<JsonRpcOptions> options)
{
    public JsonRpcParseResult Parse(ReadOnlyMemory<byte> json, string traceId)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = options.Value.MaxJsonDepth
                });

            return ParseRoot(document.RootElement, traceId);
        }
        catch (JsonException)
        {
            return SingleError(JsonRpcErrors.ParseError(traceId));
        }
    }

    private JsonRpcParseResult ParseRoot(JsonElement root, string traceId)
    {
        if (root.ValueKind is JsonValueKind.Object)
        {
            return new JsonRpcParseResult(false, [ParseRequest(root, traceId)]);
        }

        if (root.ValueKind is not JsonValueKind.Array)
        {
            return SingleError(JsonRpcErrors.InvalidRequest(traceId));
        }

        int count = root.GetArrayLength();
        if (count == 0 || count > options.Value.MaxBatchSize)
        {
            return SingleError(JsonRpcErrors.InvalidRequest(traceId));
        }

        List<JsonRpcWorkItem> items = new(count);
        foreach (JsonElement element in root.EnumerateArray())
        {
            items.Add(ParseRequest(element, traceId));
        }

        return new JsonRpcParseResult(true, items);
    }

    private static JsonRpcWorkItem ParseRequest(JsonElement element, string traceId)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            return InvalidRequest(traceId);
        }

        if (!element.TryGetProperty("jsonrpc", out JsonElement version)
            || version.ValueKind is not JsonValueKind.String
            || !string.Equals(version.GetString(), "2.0", StringComparison.Ordinal))
        {
            return InvalidRequest(traceId);
        }

        if (!element.TryGetProperty("method", out JsonElement methodElement)
            || methodElement.ValueKind is not JsonValueKind.String)
        {
            return InvalidRequest(traceId);
        }

        string method = methodElement.GetString()!;
        if (method.Length == 0 || method.StartsWith("rpc.", StringComparison.Ordinal))
        {
            return InvalidRequest(traceId);
        }

        JsonRpcIdProto id = JsonRpcIdProto.Missing;
        if (element.TryGetProperty("id", out JsonElement idElement))
        {
            if (idElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null))
            {
                return InvalidRequest(traceId);
            }

            id = JsonRpcIdProto.From(idElement);
        }

        JsonElement? parameters = null;
        if (element.TryGetProperty("params", out JsonElement paramsElement))
        {
            if (paramsElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                JsonRpcResponseEnvelope response = JsonRpcResponseEnvelope.Failure(
                    id,
                    JsonRpcErrors.InvalidParams(traceId));
                return JsonRpcWorkItem.FromError(response, !id.IsPresent, method);
            }

            parameters = paramsElement.Clone();
        }

        return JsonRpcWorkItem.FromRequest(new JsonRpcRequest(method, parameters, id));
    }

    private static JsonRpcParseResult SingleError(JsonRpcErrorPacket error) =>
        new(false, [JsonRpcWorkItem.FromError(
            JsonRpcResponseEnvelope.Failure(JsonRpcIdProto.Null, error))]);

    private static JsonRpcWorkItem InvalidRequest(string traceId) =>
        JsonRpcWorkItem.FromError(
            JsonRpcResponseEnvelope.Failure(
                JsonRpcIdProto.Null,
                JsonRpcErrors.InvalidRequest(traceId)));
}
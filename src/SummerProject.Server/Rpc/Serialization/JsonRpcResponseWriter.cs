using System.Buffers;
using System.Text.Json;

using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Rpc.Serialization;

internal sealed class JsonRpcResponseWriter
{
    public byte[] Write(IReadOnlyList<JsonRpcResponseEnvelope> responses, bool writeAsBatch)
    {
        if (responses.Count == 0)
        {
            throw new ArgumentException("하나 이상의 응답이 필요합니다.", nameof(responses));
        }

        ArrayBufferWriter<byte> buffer = new();

        using (Utf8JsonWriter writer = new(buffer))
        {
            if (writeAsBatch)
            {
                writer.WriteStartArray();
            }

            foreach (JsonRpcResponseEnvelope response in responses)
            {
                WriteResponse(writer, response);
            }

            if (writeAsBatch)
            {
                writer.WriteEndArray();
            }
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteResponse(Utf8JsonWriter writer, JsonRpcResponseEnvelope response)
    {
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");

        // 성공 응답의 result와 실패 응답의 error는 동시에 직렬화하지 않는다.
        if (response.IsError)
        {
            WriteError(writer, response.Error!);
        }
        else
        {
            writer.WritePropertyName("result");
            response.Result.WriteTo(writer);
        }

        writer.WritePropertyName("id");
        response.Id.WriteTo(writer);
        writer.WriteEndObject();
    }

    private static void WriteError(Utf8JsonWriter writer, JsonRpcErrorPacket error)
    {
        writer.WritePropertyName("error");
        writer.WriteStartObject();
        writer.WriteNumber("code", error.Code);
        writer.WriteString("message", error.Message);
        writer.WritePropertyName("data");
        writer.WriteStartObject();
        writer.WriteString("key", error.Data.Key);
        writer.WriteString("traceId", error.Data.TraceId);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
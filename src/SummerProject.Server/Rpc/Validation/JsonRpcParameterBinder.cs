using System.Text.Json;

using SummerProject.Server.Rpc.Serialization;

namespace SummerProject.Server.Rpc.Validation;

internal sealed class JsonRpcParameterBinder(JsonRpcSerializerOptions serializerOptions)
{
    public TRequest Bind<TRequest>(JsonElement? parameters, IReadOnlyList<string> parameterNames)
    {
        try
        {
            if (parameters is null)
            {
                return Deserialize<TRequest>("{}"u8);
            }

            return parameters.Value.ValueKind switch
            {
                JsonValueKind.Object => Deserialize<TRequest>(parameters.Value.GetRawText()),
                JsonValueKind.Array => BindArray<TRequest>(parameters.Value, parameterNames),
                _ => throw new JsonRpcInvalidParamsException()
            };
        }
        catch (JsonRpcInvalidParamsException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new JsonRpcInvalidParamsException(exception);
        }
    }

    private TRequest BindArray<TRequest>(JsonElement parameters, IReadOnlyList<string> parameterNames)
    {
        // 위치 기반 params는 등록 시 선언된 이름 순서와 개수가 정확히 일치해야 한다.
        if (parameters.GetArrayLength() != parameterNames.Count)
        {
            throw new JsonRpcInvalidParamsException();
        }

        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();

            int index = 0;
            foreach (JsonElement value in parameters.EnumerateArray())
            {
                writer.WritePropertyName(parameterNames[index]);
                value.WriteTo(writer);
                index++;
            }

            writer.WriteEndObject();
        }

        return Deserialize<TRequest>(stream.ToArray());
    }

    private TRequest Deserialize<TRequest>(ReadOnlySpan<byte> json)
    {
        TRequest? request = JsonSerializer.Deserialize<TRequest>(json, serializerOptions.Value);
        return request ?? throw new JsonRpcInvalidParamsException();
    }

    private TRequest Deserialize<TRequest>(string json)
    {
        TRequest? request = JsonSerializer.Deserialize<TRequest>(json, serializerOptions.Value);
        return request ?? throw new JsonRpcInvalidParamsException();
    }
}
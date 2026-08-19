using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SummerProject.Server.Rpc.Contracts;

/// <summary>
/// JSON-RPC 요청 ID의 원래 JSON 타입과 값을 보존합니다.
/// </summary>
/// <remarks>
/// 속성이 생략된 알림과 명시적인 null ID를 구분합니다.
/// </remarks>
public readonly struct JsonRpcIdProto
{
    private readonly JsonElement _value;

    private JsonRpcIdProto(JsonElement value)
    {
        IsPresent = true;
        _value = value.Clone();
    }

    public bool IsPresent { get; }

    public JsonValueKind ValueKind => IsPresent ? _value.ValueKind : JsonValueKind.Undefined;

    public static JsonRpcIdProto Missing => default;

    public static JsonRpcIdProto Null => From(JsonSerializer.SerializeToElement<object?>(null));

    public static JsonRpcIdProto From(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null))
        {
            throw new ArgumentException("JSON-RPC ID는 문자열, 숫자 또는 null이어야 합니다.", nameof(value));
        }

        return new JsonRpcIdProto(value);
    }

    internal void WriteTo(Utf8JsonWriter writer)
    {
        if (!IsPresent || _value.ValueKind is JsonValueKind.Null)
        {
            writer.WriteNullValue();
            return;
        }

        _value.WriteTo(writer);
    }

    internal string ToSafeLogValue()
    {
        if (!IsPresent)
        {
            return "missing";
        }

        if (_value.ValueKind is JsonValueKind.Null)
        {
            return "null";
        }

        if (_value.ValueKind is JsonValueKind.Number)
        {
            const int maximumNumberLength = 64;
            string rawNumber = _value.GetRawText();
            return rawNumber.Length <= maximumNumberLength
                ? rawNumber
                : rawNumber[..maximumNumberLength];
        }

        // 문자열 ID는 민감한 클라이언트 값을 포함할 수 있으므로 원문 대신 안정적인 축약 해시만 기록한다.
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(_value.GetString()!));
        return $"string:{Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant()}";
    }
}
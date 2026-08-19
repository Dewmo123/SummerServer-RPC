using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Rpc.Serialization;

internal sealed class JsonRpcSerializerOptions
{
    public JsonRpcSerializerOptions(IOptions<JsonRpcOptions> options)
    {
        // 이름 기반 params는 대소문자를 구분하고 알려지지 않은 필드를 거부한다.
        Value = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = options.Value.MaxJsonDepth,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
    }

    public JsonSerializerOptions Value { get; }
}
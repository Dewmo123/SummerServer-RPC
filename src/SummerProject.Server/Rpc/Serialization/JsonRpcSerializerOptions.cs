using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Rpc.Serialization;

internal sealed class JsonRpcSerializerOptions
{
    public JsonRpcSerializerOptions(IOptions<JsonRpcOptions> options)
    {
        Value = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = options.Value.MaxJsonDepth,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
    }

    public JsonSerializerOptions Value { get; }
}
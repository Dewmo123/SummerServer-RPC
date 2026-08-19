using System.Text.Json;

using SummerProject.Server.Rpc.Contracts;

namespace SummerProject.Server.Rpc.Serialization;

internal sealed class JsonRpcResponseEnvelope
{
    private JsonRpcResponseEnvelope(
        JsonRpcIdProto id,
        JsonElement result,
        JsonRpcErrorPacket? error)
    {
        Id = id;
        Result = result;
        Error = error;
    }

    public JsonRpcIdProto Id { get; }

    public JsonElement Result { get; }

    public JsonRpcErrorPacket? Error { get; }

    public bool IsError => Error is not null;

    public static JsonRpcResponseEnvelope Success(JsonRpcIdProto id, JsonElement result) =>
        new(id, result, null);

    public static JsonRpcResponseEnvelope Failure(JsonRpcIdProto id, JsonRpcErrorPacket error) =>
        new(id, default, error);
}
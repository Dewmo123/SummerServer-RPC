using System.Text.Json;
using System.Text.Json.Serialization;

using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Helpers.Rooms;

/// <summary>
/// user_rooms의 함정 JSON을 외부 TrapPacket과 같은 안정된 스키마로 저장하고 읽습니다.
/// </summary>
internal sealed class RoomTrapSnapshotSerializer
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public string Serialize(IReadOnlyList<TrapProto> traps)
    {
        TrapPacket[] packets = traps.Select(static trap => trap.ToPacket()).ToArray();
        return JsonSerializer.Serialize(packets, _options);
    }

    public IReadOnlyList<TrapPacket> Deserialize(string json)
    {
        TrapPacket?[] packets = JsonSerializer.Deserialize<TrapPacket?[]>(json, _options)
            ?? throw new InvalidOperationException("저장된 사용자 방 함정 스냅샷이 배열이 아닙니다.");
        if (packets.Any(static packet => packet is null))
        {
            throw new InvalidOperationException("저장된 사용자 방 함정 스냅샷에 null 요소가 있습니다.");
        }

        return packets!;
    }
}
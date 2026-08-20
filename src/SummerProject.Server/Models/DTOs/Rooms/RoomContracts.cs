using System.Collections.Immutable;

using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.Rooms;

namespace SummerProject.Server.Models.DTOs.Rooms;

internal sealed class UpsertMyRoomRequest
{
    public required long MapId { get; init; }

    public required TrapPacket[] Traps { get; init; }
}

internal sealed record UpsertMyRoomResponse(RoomPacket Room);

internal sealed class GetMyRoomRequest;

internal sealed record GetMyRoomResponse(RoomPacket Room);

/// <summary>
/// 사용자 식별자와 검증된 맵·함정 스냅샷을 함께 반환합니다.
/// </summary>
internal sealed record RoomPacket(
    long UserId,
    MapPacket Map,
    ImmutableArray<TrapPacket> Traps)
{
    public static RoomPacket From(RoomLayoutProto room) =>
        new(
            room.UserId,
            room.Map.ToPacket(),
            room.Traps.Select(static trap => trap.ToPacket()).ToImmutableArray());
}
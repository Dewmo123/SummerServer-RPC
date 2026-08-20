using System.Collections.Immutable;

using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Models.Rooms;

/// <summary>
/// 카탈로그와 함정 배치 검증을 통과한 사용자 방 전체 스냅샷입니다.
/// </summary>
internal sealed record RoomLayoutProto(
    long UserId,
    MapProto Map,
    ImmutableArray<TrapProto> Traps);
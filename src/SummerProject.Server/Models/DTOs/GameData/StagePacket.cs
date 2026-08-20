using System.Collections.Immutable;

namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// JSON-RPC 응답에 노출되는 검증된 스테이지 계약입니다.
/// </summary>
internal sealed record StagePacket(
    long StageId,
    int Width,
    int Height,
    ImmutableArray<bool> Tiles,
    ImmutableArray<TrapPacket> Traps,
    int MinimumClearSeconds,
    long RewardExp,
    long RewardGold);
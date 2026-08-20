using System.Collections.Immutable;

namespace SummerProject.Server.Models.GameData;

/// <summary>
/// 시작 시 검증되어 실행 중 변경되지 않는 스테이지 정의입니다.
/// </summary>
internal sealed record StageProto(
    long StageId,
    int Width,
    int Height,
    ImmutableArray<bool> Tiles,
    ImmutableArray<TrapProto> Traps,
    int MinimumClearSeconds,
    long RewardExp,
    long RewardGold);
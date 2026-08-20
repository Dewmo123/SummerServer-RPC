using System.Collections.Immutable;

namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// JSON-RPC 응답에 노출되는 검증된 맵 계약입니다.
/// </summary>
internal sealed record MapPacket(
    long MapId,
    int Width,
    int Height,
    ImmutableArray<bool> Tiles);
using System.Collections.Immutable;

using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// 검증된 카탈로그 Proto를 외부 JSON-RPC Packet으로 변환합니다.
/// </summary>
internal static class CatalogPacketMapper
{
    /// <summary>
    /// 내부 불변 맵을 DB 모델 노출 없이 외부 응답 계약으로 변환합니다.
    /// </summary>
    public static MapPacket ToPacket(this MapProto map) =>
        new(map.MapId, map.Width, map.Height, map.Tiles);

    /// <summary>
    /// 내부 불변 스테이지와 함정을 외부 응답 계약으로 변환합니다.
    /// </summary>
    public static StagePacket ToPacket(this StageProto stage) =>
        new(
            stage.StageId,
            stage.Width,
            stage.Height,
            stage.Tiles,
            stage.Traps.Select(static trap => new TrapPacket(
                trap.Type,
                new PositionPacket(trap.Position.X, trap.Position.Y, trap.Position.Z),
                new RotationPacket(
                    trap.Rotation.X,
                    trap.Rotation.Y,
                    trap.Rotation.Z,
                    trap.Rotation.W))).ToImmutableArray(),
            stage.MinimumClearSeconds,
            stage.RewardExp,
            stage.RewardGold);
}
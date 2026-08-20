using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// JSON-RPC 응답에서 함정 종류, 위치와 회전을 함께 전달합니다.
/// </summary>
internal sealed record TrapPacket(
    TrapTypeProto Type,
    PositionPacket Position,
    RotationPacket Rotation);
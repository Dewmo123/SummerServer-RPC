namespace SummerProject.Server.Models.GameData;

/// <summary>
/// 종류, 위치와 회전 불변 조건을 통과한 카탈로그 함정입니다.
/// </summary>
internal sealed record TrapProto(
    TrapTypeProto Type,
    GridPositionProto Position,
    NormalizedRotationProto Rotation);
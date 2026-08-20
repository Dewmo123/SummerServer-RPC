namespace SummerProject.Server.Models.GameData;

/// <summary>
/// 카탈로그 경계 검증을 통과한 정수 격자 위치입니다.
/// </summary>
internal readonly record struct GridPositionProto(int X, int Y, int Z);
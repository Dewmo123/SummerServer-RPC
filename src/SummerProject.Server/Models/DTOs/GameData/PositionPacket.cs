namespace SummerProject.Server.Models.DTOs.GameData;

/// <summary>
/// JSON-RPC 응답에서 함정의 정수 격자 위치를 표현합니다.
/// </summary>
internal sealed record PositionPacket(int X, int Y, int Z);
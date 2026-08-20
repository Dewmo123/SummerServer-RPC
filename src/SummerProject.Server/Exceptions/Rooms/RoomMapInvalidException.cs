namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 저장된 맵 ID와 현재 정적 카탈로그의 참조 무결성이 깨졌을 때 사용합니다.
/// </summary>
internal sealed class RoomMapInvalidException : Exception;
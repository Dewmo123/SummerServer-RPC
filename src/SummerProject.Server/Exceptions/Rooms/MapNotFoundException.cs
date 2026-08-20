namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 방 요청이 참조하는 맵이 정적 카탈로그에 없을 때 사용합니다.
/// </summary>
internal sealed class MapNotFoundException : Exception;
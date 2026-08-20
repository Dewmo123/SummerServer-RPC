namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 함정의 정수 좌표가 선택한 맵의 배치 범위를 벗어났을 때 사용합니다.
/// </summary>
internal sealed class TrapOutOfBoundsException : Exception;
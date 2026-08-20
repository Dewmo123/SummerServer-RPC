namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 한 방 스냅샷에 같은 격자 좌표가 둘 이상 포함되었을 때 사용합니다.
/// </summary>
internal sealed class TrapPositionDuplicatedException : Exception;
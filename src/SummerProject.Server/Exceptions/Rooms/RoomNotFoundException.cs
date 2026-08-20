namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 인증된 사용자에게 저장된 방 스냅샷이 없을 때 사용합니다.
/// </summary>
internal sealed class RoomNotFoundException : Exception;
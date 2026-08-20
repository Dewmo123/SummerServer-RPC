namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 방 요청의 필수 구조가 없거나 계약의 최대 함정 수를 초과했을 때 사용합니다.
/// </summary>
internal sealed class RoomInvalidParamsException : Exception;
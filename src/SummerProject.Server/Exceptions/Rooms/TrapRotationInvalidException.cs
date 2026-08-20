namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// quaternion 크기가 계약의 정규화 허용 범위를 벗어났을 때 사용합니다.
/// </summary>
internal sealed class TrapRotationInvalidException : Exception;
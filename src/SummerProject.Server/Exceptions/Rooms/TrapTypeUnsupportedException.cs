namespace SummerProject.Server.Exceptions.Rooms;

/// <summary>
/// 외부 요청의 함정 코드가 서버의 안정된 열거 계약에 없을 때 사용합니다.
/// </summary>
internal sealed class TrapTypeUnsupportedException : Exception;
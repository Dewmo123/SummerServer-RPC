namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 인증된 사용자가 다른 사용자의 실행을 완료하지 못하도록 소유권 실패를 구분합니다.
/// </summary>
internal sealed class StageRunForbiddenException : Exception;
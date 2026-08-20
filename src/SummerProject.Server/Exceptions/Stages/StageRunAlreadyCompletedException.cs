namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 완료 또는 포기된 실행에 보상을 다시 지급하지 않도록 상태 충돌을 표현합니다.
/// </summary>
internal sealed class StageRunAlreadyCompletedException : Exception;
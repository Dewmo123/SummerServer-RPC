namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 서버 시간 기준 최소 클리어 시간이 경과하지 않은 완료 요청을 거부합니다.
/// </summary>
internal sealed class StageClearTooEarlyException : Exception;
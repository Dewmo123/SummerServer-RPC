namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 완료할 스테이지 실행 ID가 DB에 존재하지 않을 때 사용합니다.
/// </summary>
internal sealed class StageRunNotFoundException : Exception;
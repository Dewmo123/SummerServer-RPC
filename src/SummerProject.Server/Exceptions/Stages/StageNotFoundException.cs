namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 요청 또는 실행 기록이 참조하는 스테이지가 정적 카탈로그에 없을 때 사용합니다.
/// </summary>
internal sealed class StageNotFoundException : Exception;
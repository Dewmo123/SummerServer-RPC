namespace SummerProject.Server.Models.Stages;

/// <summary>
/// DB 제약과 완료 선점 조건에서 공유하는 스테이지 실행 상태 코드입니다.
/// </summary>
internal enum StageRunStatusProto
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2
}
namespace SummerProject.Server.Exceptions.Stages;

/// <summary>
/// 완료 선점 이후 보상 처리 실패를 단일 계약 오류로 변환하고 전체 변경을 롤백합니다.
/// </summary>
internal sealed class StageRewardFailedException : Exception
{
    public StageRewardFailedException()
    {
    }

    public StageRewardFailedException(Exception innerException)
        : base("스테이지 보상 트랜잭션이 실패했습니다.", innerException)
    {
    }
}
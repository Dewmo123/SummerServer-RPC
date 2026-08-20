using SummerProject.Server.Models.Stages;

namespace SummerProject.Server.Models.Datas.Stages;

/// <summary>
/// Dapper가 stage_runs 행을 매핑하는 플레이 기록 전용 모델입니다.
/// </summary>
internal sealed class StageRunModel
{
    public long Id { get; init; }

    public long UserId { get; init; }

    public long StageId { get; init; }

    public StageRunStatusProto Status { get; init; }

    public long StartedAtUtcMs { get; init; }

    public long? CompletedAtUtcMs { get; init; }

    public long ExpGained { get; init; }

    public string? CurrenciesGainedJson { get; init; }
}
using SummerProject.Server.Models.DTOs.Characters;
using SummerProject.Server.Models.DTOs.Currencies;
using SummerProject.Server.Models.DTOs.GameData;

namespace SummerProject.Server.Models.DTOs.Stages;

internal sealed class GetStageRequest
{
    public required long StageId { get; init; }
}

internal sealed record GetStageResponse(StagePacket Stage);

internal sealed class EnterStageRequest
{
    public required long StageId { get; init; }
}

internal sealed record EnterStageResponse(
    long RunId,
    StagePacket Stage);

internal sealed class CompleteStageRequest
{
    public required long RunId { get; init; }
}

/// <summary>
/// 한 번의 완료로 획득한 값과 커밋 이후의 전체 상태를 함께 반환합니다.
/// </summary>
internal sealed record CompleteStageResponse(
    long StageId,
    long ExpGained,
    CharacterPacket Character,
    IReadOnlyList<CurrencyPacket> GainedCurrencies,
    IReadOnlyList<CurrencyPacket> AllCurrencies);
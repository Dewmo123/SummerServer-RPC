using SummerProject.Server.Models.Characters;
using SummerProject.Server.Models.Currencies;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Models.Stages;

/// <summary>
/// 새 실행 ID와 시작 시 검증된 스테이지 정의를 함께 전달합니다.
/// </summary>
internal sealed record StageEntryProto(
    long RunId,
    StageProto Stage);

/// <summary>
/// 완료 트랜잭션이 커밋한 획득량과 전체 플레이어 상태를 전달합니다.
/// </summary>
internal sealed record StageCompletionProto(
    long StageId,
    long ExpGained,
    CharacterProto Character,
    IReadOnlyList<CurrencyProto> GainedCurrencies,
    IReadOnlyList<CurrencyProto> AllCurrencies);
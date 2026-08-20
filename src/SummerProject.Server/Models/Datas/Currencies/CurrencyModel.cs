using SummerProject.Server.Models.Currencies;

namespace SummerProject.Server.Models.Datas.Currencies;

/// <summary>
/// Dapper가 currencies 행을 매핑하는 내부 모델이며 검증된 Proto로 변환해 사용합니다.
/// </summary>
internal sealed class CurrencyModel
{
    public long UserId { get; init; }

    public CurrencyTypeProto Type { get; init; }

    public long Amount { get; init; }
}
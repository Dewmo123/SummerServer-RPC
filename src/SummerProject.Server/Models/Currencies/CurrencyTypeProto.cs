namespace SummerProject.Server.Models.Currencies;

/// <summary>
/// DB와 외부 계약에서 공통으로 사용하는 지원 재화 코드를 정의합니다.
/// </summary>
public enum CurrencyTypeProto
{
    Gold = 1,
    Gem = 2,
    StageTicket = 3,
    EventToken = 4
}
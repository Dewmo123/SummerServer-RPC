namespace SummerProject.Server.Models.Currencies;

/// <summary>
/// 검증된 재화 종류와 현재 잔액을 전달합니다.
/// </summary>
public sealed record CurrencyProto(
    CurrencyTypeProto Type,
    long Amount);
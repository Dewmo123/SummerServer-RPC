using SummerProject.Server.Models.Currencies;

namespace SummerProject.Server.Models.DTOs.Currencies;

public sealed class GetMyCurrencyRequest
{
    public required CurrencyTypeProto Type { get; init; }
}

public sealed class ListMyCurrenciesRequest;

/// <summary>
/// 재화 종류를 계약에 정의된 숫자 코드로 직렬화하는 응답 구성 객체입니다.
/// </summary>
public sealed record CurrencyPacket(
    CurrencyTypeProto Type,
    long Amount)
{
    internal static CurrencyPacket From(CurrencyProto currency) =>
        new(currency.Type, currency.Amount);
}

public sealed record GetMyCurrencyResponse(CurrencyPacket Currency);

public sealed record ListMyCurrenciesResponse(IReadOnlyList<CurrencyPacket> Currencies);
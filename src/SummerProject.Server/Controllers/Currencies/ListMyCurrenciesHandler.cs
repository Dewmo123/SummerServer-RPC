using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Currencies;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Currencies;

namespace SummerProject.Server.Controllers.Currencies;

internal sealed class ListMyCurrenciesHandler(
    CallerContext callerContext,
    CurrencyQueryService currencyQueryService)
    : IRpcMethodHandler<ListMyCurrenciesRequest, ListMyCurrenciesResponse>
{
    public async ValueTask<ListMyCurrenciesResponse> HandleAsync(
        ListMyCurrenciesRequest request,
        CancellationToken cancellationToken)
    {
        // 다른 사용자 재화를 조회할 수 없도록 검증된 JWT 호출자만 조회 기준으로 사용한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        IReadOnlyList<CurrencyPacket> currencies = (await currencyQueryService.ListMineAsync(
                caller.UserId,
                cancellationToken))
            .Select(CurrencyPacket.From)
            .ToArray();
        return new ListMyCurrenciesResponse(currencies);
    }
}
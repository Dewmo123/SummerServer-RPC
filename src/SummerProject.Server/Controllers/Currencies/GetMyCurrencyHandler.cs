using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Currencies;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Currencies;

namespace SummerProject.Server.Controllers.Currencies;

internal sealed class GetMyCurrencyHandler(
    CallerContext callerContext,
    CurrencyQueryService currencyQueryService)
    : IRpcMethodHandler<GetMyCurrencyRequest, GetMyCurrencyResponse>
{
    public async ValueTask<GetMyCurrencyResponse> HandleAsync(
        GetMyCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        // 요청의 사용자 ID는 클라이언트 params가 아니라 검증된 JWT 호출자에서만 가져온다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        CurrencyPacket currency = CurrencyPacket.From(
            await currencyQueryService.GetMineAsync(
                caller.UserId,
                request.Type,
                cancellationToken));
        return new GetMyCurrencyResponse(currency);
    }
}
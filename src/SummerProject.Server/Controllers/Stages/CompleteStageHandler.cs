using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Characters;
using SummerProject.Server.Models.DTOs.Currencies;
using SummerProject.Server.Models.DTOs.Stages;
using SummerProject.Server.Models.Stages;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Stages;

namespace SummerProject.Server.Controllers.Stages;

internal sealed class CompleteStageHandler(
    CallerContext callerContext,
    StageCompletionService stageCompletionService)
    : IRpcMethodHandler<CompleteStageRequest, CompleteStageResponse>
{
    public async ValueTask<CompleteStageResponse> HandleAsync(
        CompleteStageRequest request,
        CancellationToken cancellationToken)
    {
        // 실행 ID만 클라이언트에서 받고 소유자 판정은 인증 문맥과 DB 기록으로 수행한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        StageCompletionProto completion = await stageCompletionService.CompleteAsync(
            caller.UserId,
            request.RunId,
            cancellationToken);
        return new CompleteStageResponse(
            completion.StageId,
            completion.ExpGained,
            CharacterPacket.From(completion.Character),
            completion.GainedCurrencies.Select(CurrencyPacket.From).ToArray(),
            completion.AllCurrencies.Select(CurrencyPacket.From).ToArray());
    }
}
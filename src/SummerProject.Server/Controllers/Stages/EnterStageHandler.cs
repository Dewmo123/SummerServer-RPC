using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.DTOs.Stages;
using SummerProject.Server.Models.Stages;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Stages;

namespace SummerProject.Server.Controllers.Stages;

internal sealed class EnterStageHandler(
    CallerContext callerContext,
    StageEntryService stageEntryService)
    : IRpcMethodHandler<EnterStageRequest, EnterStageResponse>
{
    public async ValueTask<EnterStageResponse> HandleAsync(
        EnterStageRequest request,
        CancellationToken cancellationToken)
    {
        // 실행 소유자는 요청 params가 아니라 검증된 JWT 호출자에서만 결정한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        StageEntryProto entry = await stageEntryService.EnterAsync(
            caller.UserId,
            request.StageId,
            cancellationToken);
        return new EnterStageResponse(entry.RunId, entry.Stage.ToPacket());
    }
}
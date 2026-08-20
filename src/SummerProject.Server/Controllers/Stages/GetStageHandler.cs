using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.DTOs.Stages;
using SummerProject.Server.Models.GameData;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Stages;

namespace SummerProject.Server.Controllers.Stages;

internal sealed class GetStageHandler(StageCatalogQueryService stageCatalogQueryService)
    : IRpcMethodHandler<GetStageRequest, GetStageResponse>
{
    public ValueTask<GetStageResponse> HandleAsync(
        GetStageRequest request,
        CancellationToken cancellationToken)
    {
        // 정적 카탈로그 조회는 계약상 인증 없이 제공하며 플레이어 상태를 변경하지 않는다.
        StageProto stage = stageCatalogQueryService.Get(request.StageId);
        return ValueTask.FromResult(new GetStageResponse(stage.ToPacket()));
    }
}
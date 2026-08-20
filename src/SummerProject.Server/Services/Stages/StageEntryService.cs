using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Models.GameData;
using SummerProject.Server.Models.Stages;
using SummerProject.Server.Repositories.Stages;

namespace SummerProject.Server.Services.Stages;

internal sealed class StageEntryService(
    StageCatalogQueryService stageCatalogQueryService,
    StageRunRepository stageRunRepository,
    TimeProvider timeProvider)
{
    public async ValueTask<StageEntryProto> EnterAsync(
        long userId,
        long stageId,
        CancellationToken cancellationToken)
    {
        // 카탈로그에 없는 ID로 실행 기록이 생기지 않도록 DB 트랜잭션보다 먼저 정적 정의를 확인한다.
        StageProto stage = stageCatalogQueryService.Get(stageId);
        StageEntryRepositoryResult result = await stageRunRepository.EnterAsync(
            userId,
            stageId,
            timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            cancellationToken);
        return result.Status switch
        {
            StageEntryRepositoryStatus.Succeeded => new StageEntryProto(result.Run!.Id, stage),
            StageEntryRepositoryStatus.UserNotFound => throw new UserNotFoundException(),
            _ => throw new InvalidOperationException("알 수 없는 스테이지 입장 결과입니다.")
        };
    }
}
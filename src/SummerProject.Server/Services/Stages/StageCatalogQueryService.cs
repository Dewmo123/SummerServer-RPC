using SummerProject.Server.Exceptions.Stages;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Services.Stages;

/// <summary>
/// 시작 시 검증된 불변 카탈로그만 조회하며 없는 ID를 스테이지 업무 오류로 변환합니다.
/// </summary>
internal sealed class StageCatalogQueryService(StageCatalog stageCatalog)
{
    public StageProto Get(long stageId) =>
        stageCatalog.TryGet(stageId, out StageProto? stage)
            ? stage
            : throw new StageNotFoundException();
}
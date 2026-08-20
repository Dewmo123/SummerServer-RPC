using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs.Stages;

/// <summary>
/// 시작 시 검증한 스테이지를 ID 기준으로 조회하는 불변 카탈로그입니다.
/// </summary>
internal sealed class StageCatalog(IEnumerable<StageProto> stages)
{
    private readonly FrozenDictionary<long, StageProto> _stages =
        stages.ToFrozenDictionary(stage => stage.StageId);

    public int Count => _stages.Count;

    public bool TryGet(long stageId, [NotNullWhen(true)] out StageProto? stage) =>
        _stages.TryGetValue(stageId, out stage);
}
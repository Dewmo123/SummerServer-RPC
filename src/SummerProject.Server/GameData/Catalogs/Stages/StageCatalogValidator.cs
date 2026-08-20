using System.Collections.Immutable;

using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs.Stages;

/// <summary>
/// 스테이지와 모든 함정을 검증한 뒤 불변 스테이지 값으로 변환합니다.
/// </summary>
internal static class StageCatalogValidator
{
    public static StageProto Validate(StageCatalogDocument document, string source)
    {
        if (document.StageId <= 0)
        {
            throw Invalid(source, "stageId는 1 이상이어야 합니다.");
        }

        if (document.Width <= 0 || document.Height <= 0)
        {
            throw Invalid(source, "width와 height는 1 이상이어야 합니다.");
        }

        if (document.Tiles is null)
        {
            throw Invalid(source, "tiles 배열은 필수입니다.");
        }

        if (document.Traps is null)
        {
            throw Invalid(source, "traps 배열은 필수입니다.");
        }

        if (document.MinimumClearSeconds < 0)
        {
            throw Invalid(source, "minimumClearSeconds는 0 이상이어야 합니다.");
        }

        if (document.RewardExp < 0 || document.RewardGold < 0)
        {
            throw Invalid(source, "rewardExp와 rewardGold는 0 이상이어야 합니다.");
        }

        ImmutableArray<TrapProto>.Builder traps = ImmutableArray.CreateBuilder<TrapProto>(document.Traps.Length);
        HashSet<GridPositionProto> positions = [];
        for (int index = 0; index < document.Traps.Length; index++)
        {
            TrapCatalogDocument trapDocument = document.Traps[index]
                ?? throw Invalid(source, $"traps[{index}]는 null일 수 없습니다.");
            TrapProto trap = TrapCatalogValidator.Validate(
                trapDocument,
                document.Width,
                document.Height,
                source,
                index);
            if (!positions.Add(trap.Position))
            {
                throw Invalid(source, $"traps[{index}]의 위치가 중복되었습니다.");
            }

            traps.Add(trap);
        }

        // 타일 개수와 면적의 일치 규칙은 미결정이므로 배열 존재 여부만 검증한다.
        return new StageProto(
            document.StageId,
            document.Width,
            document.Height,
            document.Tiles.ToImmutableArray(),
            traps.MoveToImmutable(),
            document.MinimumClearSeconds,
            document.RewardExp,
            document.RewardGold);
    }

    private static CatalogValidationException Invalid(string source, string reason) =>
        new($"스테이지 카탈로그가 유효하지 않습니다: {source} ({reason})");
}
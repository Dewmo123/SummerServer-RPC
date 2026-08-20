using System.Collections.Immutable;

using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs.Maps;

/// <summary>
/// 검증 전 맵 문서를 불변 맵 값으로 변환합니다.
/// </summary>
internal static class MapCatalogValidator
{
    public static MapProto Validate(MapCatalogDocument document, string source)
    {
        if (document.MapId <= 0)
        {
            throw Invalid(source, "mapId는 1 이상이어야 합니다.");
        }

        if (document.Width <= 0 || document.Height <= 0)
        {
            throw Invalid(source, "width와 height는 1 이상이어야 합니다.");
        }

        if (document.Tiles is null)
        {
            throw Invalid(source, "tiles 배열은 필수입니다.");
        }

        // 타일 개수와 면적의 일치 규칙은 미결정이므로 배열 존재 여부만 검증한다.
        return new MapProto(
            document.MapId,
            document.Width,
            document.Height,
            document.Tiles.ToImmutableArray());
    }

    private static CatalogValidationException Invalid(string source, string reason) =>
        new($"맵 카탈로그가 유효하지 않습니다: {source} ({reason})");
}
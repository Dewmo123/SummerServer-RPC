using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs.Maps;

/// <summary>
/// 시작 시 검증한 맵을 ID 기준으로 조회하는 불변 카탈로그입니다.
/// </summary>
internal sealed class MapCatalog(IEnumerable<MapProto> maps)
{
    private readonly FrozenDictionary<long, MapProto> _maps =
        maps.ToFrozenDictionary(map => map.MapId);

    public int Count => _maps.Count;

    public bool TryGet(long mapId, [NotNullWhen(true)] out MapProto? map) =>
        _maps.TryGetValue(mapId, out map);
}
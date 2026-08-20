using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs;

/// <summary>
/// 배포된 JSON 파일 전체를 엄격하게 역직렬화하고 검증된 카탈로그로 변환합니다.
/// </summary>
internal sealed class JsonCatalogLoader
{
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public JsonCatalogLoader(IOptions<CatalogOptions> options, IHostEnvironment environment)
        : this(options.Value, environment.ContentRootPath)
    {
    }

    internal JsonCatalogLoader(CatalogOptions options, string contentRootPath)
    {
        try
        {
            _rootPath = Path.GetFullPath(
                Path.IsPathFullyQualified(options.RootPath)
                    ? options.RootPath
                    : Path.Combine(contentRootPath, options.RootPath));
        }
        catch (Exception exception) when (exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            throw new CatalogValidationException("정적 카탈로그 루트 경로를 해석할 수 없습니다.");
        }
    }

    public MapCatalog LoadMapCatalog()
    {
        IReadOnlyList<CatalogSource<MapCatalogDocument>> sources =
            ReadDocuments<MapCatalogDocument>("Maps");
        List<MapProto> maps = new(sources.Count);
        Dictionary<long, string> idSources = [];

        foreach (CatalogSource<MapCatalogDocument> source in sources)
        {
            MapProto map = MapCatalogValidator.Validate(source.Document, source.Name);
            EnsureUniqueId("mapId", map.MapId, source.Name, idSources);
            maps.Add(map);
        }

        return new MapCatalog(maps);
    }

    public StageCatalog LoadStageCatalog()
    {
        IReadOnlyList<CatalogSource<StageCatalogDocument>> sources =
            ReadDocuments<StageCatalogDocument>("Stages");
        List<StageProto> stages = new(sources.Count);
        Dictionary<long, string> idSources = [];

        foreach (CatalogSource<StageCatalogDocument> source in sources)
        {
            StageProto stage = StageCatalogValidator.Validate(source.Document, source.Name);
            EnsureUniqueId("stageId", stage.StageId, source.Name, idSources);
            stages.Add(stage);
        }

        return new StageCatalog(stages);
    }

    private IReadOnlyList<CatalogSource<TDocument>> ReadDocuments<TDocument>(string category)
        where TDocument : class
    {
        string directoryPath = Path.Combine(_rootPath, category);
        if (!Directory.Exists(directoryPath))
        {
            throw new CatalogValidationException($"정적 카탈로그 디렉터리를 찾을 수 없습니다: {category}");
        }

        string[] filePaths;
        try
        {
            filePaths = Directory
                .EnumerateFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CatalogValidationException($"정적 카탈로그 디렉터리를 읽을 수 없습니다: {category}");
        }

        if (filePaths.Length == 0)
        {
            throw new CatalogValidationException($"정적 카탈로그 디렉터리가 비어 있습니다: {category}");
        }

        List<CatalogSource<TDocument>> documents = new(filePaths.Length);
        foreach (string filePath in filePaths)
        {
            string sourceName = $"{category}/{Path.GetFileName(filePath)}";
            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new CatalogValidationException($"정적 카탈로그 파일을 읽을 수 없습니다: {sourceName}");
            }

            TDocument? document;
            try
            {
                document = JsonSerializer.Deserialize<TDocument>(json, _serializerOptions);
            }
            catch (JsonException)
            {
                // 운영 로그에 전체 배포 경로가 노출되지 않도록 카탈로그 상대 파일명만 전달한다.
                throw new CatalogValidationException($"정적 카탈로그 JSON이 유효하지 않습니다: {sourceName}");
            }

            if (document is null)
            {
                throw new CatalogValidationException($"정적 카탈로그 JSON 객체가 비어 있습니다: {sourceName}");
            }

            documents.Add(new CatalogSource<TDocument>(sourceName, document));
        }

        return documents;
    }

    private static void EnsureUniqueId(
        string idName,
        long id,
        string sourceName,
        IDictionary<long, string> idSources)
    {
        if (idSources.TryGetValue(id, out string? previousSource))
        {
            throw new CatalogValidationException(
                $"정적 카탈로그 {idName}가 중복되었습니다: {id} ({previousSource}, {sourceName})");
        }

        idSources.Add(id, sourceName);
    }

    private sealed record CatalogSource<TDocument>(string Name, TDocument Document);
}
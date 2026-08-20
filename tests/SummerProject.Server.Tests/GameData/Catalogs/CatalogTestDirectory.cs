using System.Text.Json;

namespace SummerProject.Server.Tests.GameData.Catalogs;

internal sealed class CatalogTestDirectory : IDisposable
{
    private static readonly string TestRootPath = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "SummerProject.Server.Tests", "Catalogs"));
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private int _disposed;

    public CatalogTestDirectory(bool writeValidCatalogs = true)
    {
        RootPath = Path.Combine(TestRootPath, Guid.NewGuid().ToString("N"));
        MapsPath = Path.Combine(RootPath, "Maps");
        StagesPath = Path.Combine(RootPath, "Stages");
        Directory.CreateDirectory(MapsPath);
        Directory.CreateDirectory(StagesPath);

        if (writeValidCatalogs)
        {
            WriteMap("Map1.json", 1);
            WriteStage("Stage1.json", 1);
        }
    }

    public string RootPath { get; }

    public string MapsPath { get; }

    public string StagesPath { get; }

    public void WriteMap(string fileName, long mapId)
    {
        WriteJson(
            Path.Combine(MapsPath, fileName),
            new
            {
                mapId,
                width = 16,
                height = 8,
                tiles = new[] { true, false }
            });
    }

    public void WriteStage(string fileName, long stageId, object[]? traps = null)
    {
        WriteJson(
            Path.Combine(StagesPath, fileName),
            new
            {
                stageId,
                width = 16,
                height = 8,
                tiles = new[] { true, false },
                traps = traps ?? [CreateTrap()],
                minimumClearSeconds = 1,
                rewardExp = 10,
                rewardGold = 100
            });
    }

    public void WriteRawMap(string fileName, string json) =>
        File.WriteAllText(Path.Combine(MapsPath, fileName), json);

    public void WriteRawStage(string fileName, string json) =>
        File.WriteAllText(Path.Combine(StagesPath, fileName), json);

    public static object CreateTrap(
        int type = 0,
        int x = 3,
        int y = 0,
        int z = 0,
        double rotationW = 1.0) =>
        new
        {
            type,
            position = new { x, y, z },
            rotation = new { x = 0.0, y = 0.0, z = 0.0, w = rotationW }
        };

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0 || !Directory.Exists(RootPath))
        {
            return;
        }

        string resolvedPath = Path.GetFullPath(RootPath);
        string requiredPrefix = TestRootPath.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!resolvedPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("카탈로그 테스트 디렉터리가 허용된 임시 경로를 벗어났습니다.");
        }

        Directory.Delete(resolvedPath, recursive: true);
    }

    private static void WriteJson(string path, object value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, SerializerOptions));
}
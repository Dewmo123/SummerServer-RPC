using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.GameData.Catalogs.Maps;
using SummerProject.Server.GameData.Catalogs.Stages;
using SummerProject.Server.Models.DTOs.GameData;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.Tests.GameData.Catalogs;

public sealed class CatalogLoaderTests
{
    [Fact]
    public void ValidCatalogsLoadAsImmutableProtosAndContractPackets()
    {
        using CatalogTestDirectory directory = new();
        JsonCatalogLoader loader = CreateLoader(directory);

        MapCatalog maps = loader.LoadMapCatalog();
        StageCatalog stages = loader.LoadStageCatalog();

        Assert.Equal(1, maps.Count);
        Assert.True(maps.TryGet(1, out MapProto? map));
        Assert.Equal([true, false], map.Tiles.ToArray());
        MapPacket mapPacket = map.ToPacket();
        Assert.Equal(1, mapPacket.MapId);

        Assert.Equal(1, stages.Count);
        Assert.True(stages.TryGet(1, out StageProto? stage));
        Assert.Equal(10, stage.RewardExp);
        Assert.Equal(100, stage.RewardGold);
        Assert.Single(stage.Traps);
        Assert.Equal(TrapTypeProto.SawTrap, stage.Traps[0].Type);
        StagePacket stagePacket = stage.ToPacket();
        Assert.Equal(3, stagePacket.Traps[0].Position.X);
    }

    [Fact]
    public void CatalogIndexesByIdRegardlessOfFileNameOrder()
    {
        using CatalogTestDirectory directory = new(writeValidCatalogs: false);
        directory.WriteMap("a-second.json", 2);
        directory.WriteMap("z-first.json", 1);

        MapCatalog catalog = CreateLoader(directory).LoadMapCatalog();

        Assert.Equal(2, catalog.Count);
        Assert.True(catalog.TryGet(1, out MapProto? first));
        Assert.Equal(1, first.MapId);
        Assert.True(catalog.TryGet(2, out MapProto? second));
        Assert.Equal(2, second.MapId);
    }

    [Fact]
    public void EmptyCatalogDirectoryFailsValidation()
    {
        using CatalogTestDirectory directory = new(writeValidCatalogs: false);

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(
            () => CreateLoader(directory).LoadMapCatalog());

        Assert.Contains("비어 있습니다: Maps", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedJsonReportsRelativeFileWithoutAbsoluteRoot()
    {
        using CatalogTestDirectory directory = new(writeValidCatalogs: false);
        directory.WriteRawMap("broken.json", "{ invalid }");

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(
            () => CreateLoader(directory).LoadMapCatalog());

        Assert.Contains("Maps/broken.json", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(directory.RootPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownOrWrongCaseJsonPropertyFailsValidation()
    {
        using CatalogTestDirectory directory = new(writeValidCatalogs: false);
        directory.WriteRawMap(
            "wrong-case.json",
            """{"MapId":1,"width":16,"height":8,"tiles":[],"unknown":true}""");

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(
            () => CreateLoader(directory).LoadMapCatalog());

        Assert.Contains("Maps/wrong-case.json", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateIdsFailValidation()
    {
        using CatalogTestDirectory directory = new(writeValidCatalogs: false);
        directory.WriteStage("first.json", 7);
        directory.WriteStage("second.json", 7);

        CatalogValidationException exception = Assert.Throws<CatalogValidationException>(
            () => CreateLoader(directory).LoadStageCatalog());

        Assert.Contains("stageId가 중복되었습니다: 7", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Stages/first.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Stages/second.json", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRejectsInvalidIdSizeAndMissingTiles()
    {
        MapCatalogDocument invalidId = ValidMapDocument();
        invalidId.MapId = 0;
        Assert.Throws<CatalogValidationException>(() => MapCatalogValidator.Validate(invalidId, "Maps/id.json"));

        MapCatalogDocument invalidSize = ValidMapDocument();
        invalidSize.Width = 0;
        Assert.Throws<CatalogValidationException>(() => MapCatalogValidator.Validate(invalidSize, "Maps/size.json"));

        MapCatalogDocument missingTiles = ValidMapDocument();
        missingTiles.Tiles = null;
        Assert.Throws<CatalogValidationException>(() => MapCatalogValidator.Validate(missingTiles, "Maps/tiles.json"));
    }

    [Fact]
    public void StageRejectsNegativeTimeAndRewards()
    {
        StageCatalogDocument invalidTime = ValidStageDocument();
        invalidTime.MinimumClearSeconds = -1;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidTime, "Stages/time.json"));

        StageCatalogDocument invalidExp = ValidStageDocument();
        invalidExp.RewardExp = -1;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidExp, "Stages/exp.json"));

        StageCatalogDocument invalidGold = ValidStageDocument();
        invalidGold.RewardGold = -1;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidGold, "Stages/gold.json"));
    }

    [Fact]
    public void StageRejectsInvalidIdSizeAndMissingArrays()
    {
        StageCatalogDocument invalidId = ValidStageDocument();
        invalidId.StageId = 0;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidId, "Stages/id.json"));

        StageCatalogDocument invalidSize = ValidStageDocument();
        invalidSize.Height = 0;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidSize, "Stages/size.json"));

        StageCatalogDocument missingTiles = ValidStageDocument();
        missingTiles.Tiles = null;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(missingTiles, "Stages/tiles.json"));

        StageCatalogDocument missingTraps = ValidStageDocument();
        missingTraps.Traps = null;
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(missingTraps, "Stages/traps.json"));
    }

    [Fact]
    public void StageRejectsUnsupportedOutOfBoundsDuplicateAndInvalidRotationTraps()
    {
        StageCatalogDocument unsupported = ValidStageDocument(TrapDocument(type: 99));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(unsupported, "Stages/type.json"));

        StageCatalogDocument outOfBounds = ValidStageDocument(TrapDocument(x: 16));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(outOfBounds, "Stages/bounds.json"));

        StageCatalogDocument invalidY = ValidStageDocument(TrapDocument(y: -1));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidY, "Stages/y.json"));

        StageCatalogDocument invalidZ = ValidStageDocument(TrapDocument(z: 1));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidZ, "Stages/z.json"));

        TrapCatalogDocument duplicatedTrap = TrapDocument();
        StageCatalogDocument duplicated = ValidStageDocument(duplicatedTrap, TrapDocument());
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(duplicated, "Stages/duplicate.json"));

        StageCatalogDocument invalidRotation = ValidStageDocument(TrapDocument(rotationW: 0.5));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(invalidRotation, "Stages/rotation.json"));

        StageCatalogDocument excessiveRotation = ValidStageDocument(TrapDocument(rotationW: Math.Sqrt(1.0201)));
        Assert.Throws<CatalogValidationException>(() => StageCatalogValidator.Validate(excessiveRotation, "Stages/rotation-high.json"));
    }

    [Theory]
    [InlineData(0.98)]
    [InlineData(1.02)]
    public void RotationMagnitudeSquaredBoundaryIsAccepted(double magnitudeSquared)
    {
        StageCatalogDocument document = ValidStageDocument(
            TrapDocument(rotationW: Math.Sqrt(magnitudeSquared)));

        StageProto stage = StageCatalogValidator.Validate(document, "Stages/boundary.json");

        Assert.Single(stage.Traps);
    }

    [Fact]
    public void TileCountIsNotComparedWithAreaWhilePolicyIsUndecided()
    {
        MapCatalogDocument mapDocument = ValidMapDocument();
        mapDocument.Tiles = [true];
        StageCatalogDocument stageDocument = ValidStageDocument();
        stageDocument.Tiles = [];

        MapProto map = MapCatalogValidator.Validate(mapDocument, "Maps/tiles.json");
        StageProto stage = StageCatalogValidator.Validate(stageDocument, "Stages/tiles.json");

        Assert.Single(map.Tiles);
        Assert.Empty(stage.Tiles);
    }

    private static JsonCatalogLoader CreateLoader(CatalogTestDirectory directory) =>
        new(new CatalogOptions { RootPath = directory.RootPath }, directory.RootPath);

    private static MapCatalogDocument ValidMapDocument() =>
        new()
        {
            MapId = 1,
            Width = 16,
            Height = 8,
            Tiles = [true, false]
        };

    private static StageCatalogDocument ValidStageDocument(params TrapCatalogDocument[] traps) =>
        new()
        {
            StageId = 1,
            Width = 16,
            Height = 8,
            Tiles = [true, false],
            Traps = traps.Length == 0 ? [TrapDocument()] : traps,
            MinimumClearSeconds = 1,
            RewardExp = 10,
            RewardGold = 100
        };

    private static TrapCatalogDocument TrapDocument(
        int type = 0,
        int x = 3,
        int y = 0,
        int z = 0,
        double rotationW = 1.0) =>
        new()
        {
            Type = type,
            Position = new PositionCatalogDocument { X = x, Y = y, Z = z },
            Rotation = new RotationCatalogDocument { W = rotationW }
        };
}
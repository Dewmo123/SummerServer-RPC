namespace SummerProject.Server.GameData.Catalogs;

// 파일 계약은 검증 전 nullable 상태를 허용하고, 검증이 끝난 값만 Proto로 변환한다.
internal sealed class MapCatalogDocument
{
    public long MapId { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public bool[]? Tiles { get; set; }
}

internal sealed class StageCatalogDocument
{
    public long StageId { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public bool[]? Tiles { get; set; }

    public TrapCatalogDocument?[]? Traps { get; set; }

    public int MinimumClearSeconds { get; set; }

    public long RewardExp { get; set; }

    public long RewardGold { get; set; }
}

internal sealed class TrapCatalogDocument
{
    public int Type { get; set; }

    public PositionCatalogDocument? Position { get; set; }

    public RotationCatalogDocument? Rotation { get; set; }
}

internal sealed class PositionCatalogDocument
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Z { get; set; }
}

internal sealed class RotationCatalogDocument
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }

    public double W { get; set; }
}
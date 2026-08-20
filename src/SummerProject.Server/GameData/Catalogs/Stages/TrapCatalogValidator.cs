using SummerProject.Server.GameData.Catalogs;
using SummerProject.Server.Models.GameData;

namespace SummerProject.Server.GameData.Catalogs.Stages;

/// <summary>
/// 함정 종류, 격자 위치와 quaternion 회전을 카탈로그 경계에서 검증합니다.
/// </summary>
internal static class TrapCatalogValidator
{
    private const double MinimumRotationMagnitudeSquared = 0.98;
    private const double MaximumRotationMagnitudeSquared = 1.02;

    public static TrapProto Validate(
        TrapCatalogDocument document,
        int width,
        int height,
        string source,
        int index)
    {
        if (!Enum.IsDefined((TrapTypeProto)document.Type))
        {
            throw Invalid(source, index, "지원하지 않는 함정 종류입니다.");
        }

        if (document.Position is null)
        {
            throw Invalid(source, index, "position은 필수입니다.");
        }

        if (document.Rotation is null)
        {
            throw Invalid(source, index, "rotation은 필수입니다.");
        }

        PositionCatalogDocument position = document.Position;
        if (position.X < 0
            || position.X >= width
            || position.Y < 0
            || position.Y >= height
            || position.Z != 0)
        {
            throw Invalid(source, index, "함정 위치가 스테이지 경계를 벗어났습니다.");
        }

        RotationCatalogDocument rotation = document.Rotation;
        double magnitudeSquared = (rotation.X * rotation.X)
            + (rotation.Y * rotation.Y)
            + (rotation.Z * rotation.Z)
            + (rotation.W * rotation.W);
        if (!double.IsFinite(magnitudeSquared)
            || magnitudeSquared < MinimumRotationMagnitudeSquared
            || magnitudeSquared > MaximumRotationMagnitudeSquared)
        {
            throw Invalid(source, index, "함정 회전값이 정규화 허용 범위를 벗어났습니다.");
        }

        return new TrapProto(
            (TrapTypeProto)document.Type,
            new GridPositionProto(position.X, position.Y, position.Z),
            new NormalizedRotationProto(rotation.X, rotation.Y, rotation.Z, rotation.W));
    }

    private static CatalogValidationException Invalid(string source, int index, string reason) =>
        new($"스테이지 카탈로그의 함정이 유효하지 않습니다: {source}, traps[{index}] ({reason})");
}
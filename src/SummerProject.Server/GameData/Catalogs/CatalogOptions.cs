namespace SummerProject.Server.GameData.Catalogs;

/// <summary>
/// 배포된 맵과 스테이지 JSON을 찾기 위한 카탈로그 경로를 정의합니다.
/// </summary>
internal sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    public string RootPath { get; set; } = "GameData/Catalogs";
}
namespace SummerProject.Server.GameData.Catalogs;

/// <summary>
/// 정적 카탈로그가 시작 시 불변 조건을 만족하지 못했음을 나타냅니다.
/// </summary>
internal sealed class CatalogValidationException : Exception
{
    public CatalogValidationException(string message)
        : base(message)
    {
    }
}
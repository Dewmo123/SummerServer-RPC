namespace SummerProject.Server.Exceptions.Currencies;

/// <summary>
/// 계약에 정의되지 않은 재화 코드가 업무 계층으로 전달된 경우 사용합니다.
/// </summary>
internal sealed class CurrencyInvalidTypeException : Exception;
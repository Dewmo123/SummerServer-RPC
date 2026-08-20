namespace SummerProject.Server.Exceptions.Currencies;

/// <summary>
/// 원자적 차감 조건을 만족할 만큼 재화 잔액이 없을 때 사용합니다.
/// </summary>
internal sealed class CurrencyInsufficientException : Exception;
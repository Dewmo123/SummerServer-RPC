namespace SummerProject.Server.Exceptions.Currencies;

/// <summary>
/// 재화 증가 결과가 Int64 저장 범위를 넘을 때 기존 잔액을 보존합니다.
/// </summary>
internal sealed class CurrencyOverflowException : Exception;
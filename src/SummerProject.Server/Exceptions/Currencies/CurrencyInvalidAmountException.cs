namespace SummerProject.Server.Exceptions.Currencies;

/// <summary>
/// 재화 변경량이 양수라는 업무 불변 조건을 위반한 경우 사용합니다.
/// </summary>
internal sealed class CurrencyInvalidAmountException : Exception;
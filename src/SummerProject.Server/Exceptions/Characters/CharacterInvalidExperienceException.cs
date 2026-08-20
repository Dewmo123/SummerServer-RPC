namespace SummerProject.Server.Exceptions.Characters;

/// <summary>
/// 양수가 아니거나 안전한 성장 범위를 넘는 경험치 지급을 거부합니다.
/// </summary>
internal sealed class CharacterInvalidExperienceException : Exception;
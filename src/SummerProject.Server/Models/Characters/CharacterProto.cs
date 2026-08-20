namespace SummerProject.Server.Models.Characters;

/// <summary>
/// 검증된 캐릭터 성장 상태를 계층 간에 전달합니다.
/// </summary>
public sealed record CharacterProto(
    long Level,
    long Exp,
    long ExpToNextLevel);
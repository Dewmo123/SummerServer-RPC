using SummerProject.Server.Models.Characters;

namespace SummerProject.Server.Models.DTOs.Characters;

public sealed class GetMyCharacterRequest;

/// <summary>
/// 현재 레벨 내 경험치와 다음 레벨 요구량을 함께 반환하는 캐릭터 응답 구성 객체입니다.
/// </summary>
public sealed record CharacterPacket(
    long Level,
    long Exp,
    long ExpToNextLevel)
{
    internal static CharacterPacket From(CharacterProto character) =>
        new(character.Level, character.Exp, character.ExpToNextLevel);
}

public sealed record GetMyCharacterResponse(CharacterPacket Character);
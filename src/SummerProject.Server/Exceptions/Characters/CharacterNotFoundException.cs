namespace SummerProject.Server.Exceptions.Characters;

/// <summary>
/// 사용자가 존재하지만 캐릭터 지연 생성 결과를 확인할 수 없을 때 사용합니다.
/// </summary>
internal sealed class CharacterNotFoundException : Exception;
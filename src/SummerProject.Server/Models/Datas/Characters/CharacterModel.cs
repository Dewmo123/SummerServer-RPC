namespace SummerProject.Server.Models.Datas.Characters;

/// <summary>
/// Dapper가 characters 행을 매핑하는 내부 모델이며 RPC 응답으로 직접 노출하지 않습니다.
/// </summary>
internal sealed class CharacterModel
{
    public long UserId { get; init; }

    public long Level { get; init; }

    public long Exp { get; init; }
}
namespace SummerProject.Server.Models.Datas.Rooms;

/// <summary>
/// Dapper가 user_rooms 행에 매핑하는 사용자 방 저장 모델입니다.
/// </summary>
internal sealed class UserRoomModel
{
    public long UserId { get; init; }

    public long MapId { get; init; }

    public required string TrapsJson { get; init; }

    public long UpdatedAtUtcMs { get; init; }
}
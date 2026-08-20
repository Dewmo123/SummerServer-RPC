using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Rooms;
using SummerProject.Server.Models.Rooms;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Rooms;

namespace SummerProject.Server.Controllers.Rooms;

/// <summary>
/// 인증된 호출자의 방 저장 요청을 검증·저장 서비스에 연결합니다.
/// </summary>
internal sealed class UpsertMyRoomHandler(
    CallerContext callerContext,
    RoomLayoutService roomLayoutService)
    : IRpcMethodHandler<UpsertMyRoomRequest, UpsertMyRoomResponse>
{
    public async ValueTask<UpsertMyRoomResponse> HandleAsync(
        UpsertMyRoomRequest request,
        CancellationToken cancellationToken)
    {
        // 저장 대상 사용자는 params가 아니라 검증된 JWT 호출자에서만 결정한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        RoomLayoutProto room = await roomLayoutService.UpsertAsync(
            caller.UserId,
            request.MapId,
            request.Traps,
            cancellationToken);
        return new UpsertMyRoomResponse(RoomPacket.From(room));
    }
}
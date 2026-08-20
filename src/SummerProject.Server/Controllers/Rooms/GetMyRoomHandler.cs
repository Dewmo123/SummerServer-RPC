using SummerProject.Server.Infrastructure.Security;
using SummerProject.Server.Models.Auth;
using SummerProject.Server.Models.DTOs.Rooms;
using SummerProject.Server.Models.Rooms;
using SummerProject.Server.Rpc.Dispatching;
using SummerProject.Server.Services.Rooms;

namespace SummerProject.Server.Controllers.Rooms;

/// <summary>
/// 인증된 호출자의 저장 방만 조회 서비스에 요청합니다.
/// </summary>
internal sealed class GetMyRoomHandler(
    CallerContext callerContext,
    RoomLayoutService roomLayoutService)
    : IRpcMethodHandler<GetMyRoomRequest, GetMyRoomResponse>
{
    public async ValueTask<GetMyRoomResponse> HandleAsync(
        GetMyRoomRequest request,
        CancellationToken cancellationToken)
    {
        // 다른 사용자 ID를 받을 필드를 두지 않아 인증 주체의 방만 조회한다.
        CallerProto caller = callerContext.Caller
            ?? throw new InvalidOperationException("인증된 호출자가 없습니다.");
        RoomLayoutProto room = await roomLayoutService.GetAsync(
            caller.UserId,
            cancellationToken);
        return new GetMyRoomResponse(RoomPacket.From(room));
    }
}